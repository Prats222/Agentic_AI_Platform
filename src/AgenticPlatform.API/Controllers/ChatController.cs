using System.Text;
using System.Text.Json;
using AgenticPlatform.API.Extensions;
using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.AI;
using AgenticPlatform.Core.DTOs.Chat;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.LLM;
using AgenticPlatform.Infrastructure.Services.LLM;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
[Route("api/v{version:apiVersion}/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IAISettingsService _aiSettingsService;
    private readonly ILLMProviderFactory _llmProviderFactory;
    private readonly IWebSearchService _webSearchService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IAISettingsService aiSettingsService,
        ILLMProviderFactory llmProviderFactory,
        IWebSearchService webSearchService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _aiSettingsService = aiSettingsService;
        _llmProviderFactory = llmProviderFactory;
        _webSearchService = webSearchService;
        _logger = logger;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ChatConversationDto>>>> GetConversations(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var conversations = await _chatService.GetConversationsAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ChatConversationDto>>.Ok(conversations));
    }

    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        await _chatService.DeleteConversationAsync(conversationId, userId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPost("stream")]
    public async Task Stream(StreamChatMessageDto request, CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Append("X-Accel-Buffering", "no");

        var userId = User.GetUserId();
        if (!userId.HasValue || string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length > 8000)
        {
            await WriteEventAsync("error", new { message = "A valid message of at most 8,000 characters is required." }, cancellationToken);
            return;
        }

        try
        {
            var conversation = await _chatService.GetOrCreateConversationAsync(
                request.ConversationId,
                userId.Value,
                request.Prompt,
                request.Provider,
                request.Model,
                cancellationToken);
            conversation.Provider = request.Provider;
            conversation.Model = request.Model.Trim();
            await _chatService.AddMessageAsync(conversation, "user", request.Prompt, cancellationToken);
            await WriteEventAsync("conversation", new
            {
                conversationId = conversation.Id,
                title = conversation.Title
            }, cancellationToken);

            var messages = conversation.Messages
                .OrderBy(item => item.CreatedAt)
                .TakeLast(20)
                .Select(item => new LLMChatMessage { Role = item.Role, Content = item.Content })
                .ToList();
            if (messages.Count == 0 || messages[^1].Role != "user")
            {
                messages.Add(new LLMChatMessage { Role = "user", Content = request.Prompt });
            }

            if (_webSearchService.ShouldSearch(request.Prompt))
            {
                await WriteEventAsync("status", new { message = "Searching the live web..." }, cancellationToken);
                var searchResult = await _webSearchService.SearchAsync(request.Prompt, cancellationToken);
                if (searchResult is not null)
                {
                    messages[^1].Content = $"""
                    {messages[^1].Content}

                    Current web context supplied by {searchResult.Provider}:
                    {searchResult.Context}

                    Answer using this current context and include source URLs where available.
                    """;
                    await WriteEventAsync("search", new { provider = searchResult.Provider }, cancellationToken);
                }
                else
                {
                    messages[^1].Content += "\n\nLive search is temporarily unavailable. Be explicit that current facts could not be verified.";
                }
            }

            var directRequest = new DirectChatDto
            {
                Provider = request.Provider,
                Model = request.Model,
                Prompt = request.Prompt,
                Temperature = request.Temperature,
                MaxTokens = Math.Clamp(request.MaxTokens, 1, 4096),
                TopP = request.TopP,
                SystemPrompt = request.SystemPrompt,
                BaseUrl = request.BaseUrl
            };
            var chatRequest = await _aiSettingsService.BuildDirectChatRequestAsync(directRequest, cancellationToken);
            chatRequest.Messages = messages;
            var provider = _llmProviderFactory.GetProvider(chatRequest.Provider);
            var responseBuilder = new StringBuilder();

            await WriteEventAsync("status", new { message = "Generating response..." }, cancellationToken);
            await foreach (var chunk in provider.StreamChatAsync(chatRequest, cancellationToken))
            {
                responseBuilder.Append(chunk.Content);
                await WriteEventAsync("delta", new { content = chunk.Content }, cancellationToken);
            }

            var content = responseBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("The model completed without returning text. Please retry or choose another model.");
            }

            await _chatService.AddMessageAsync(conversation, "assistant", content, cancellationToken);
            await WriteEventAsync("done", new
            {
                conversationId = conversation.Id,
                provider = request.Provider.ToString(),
                model = request.Model
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Chat stream was cancelled by the client.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Chat streaming failed.");
            await WriteEventAsync("error", new { message = GetSafeMessage(exception) }, CancellationToken.None);
        }
    }

    private async Task WriteEventAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static string GetSafeMessage(Exception exception)
    {
        return exception switch
        {
            LLMProviderException => exception.Message,
            KeyNotFoundException => exception.Message,
            InvalidOperationException => exception.Message,
            _ => "The chat request failed. Please retry shortly."
        };
    }
}
