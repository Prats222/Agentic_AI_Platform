using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.AI;
using AgenticPlatform.Core.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
[Route("api/v{version:apiVersion}/ai-settings")]
public sealed class AISettingsController : ControllerBase
{
    private readonly IAISettingsService _aiSettingsService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILLMProviderFactory _llmProviderFactory;

    public AISettingsController(
        IAISettingsService aiSettingsService,
        IHttpClientFactory httpClientFactory,
        ILLMProviderFactory llmProviderFactory)
    {
        _aiSettingsService = aiSettingsService;
        _httpClientFactory = httpClientFactory;
        _llmProviderFactory = llmProviderFactory;
    }

    [HttpGet]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(ApiResponse<AISettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AISettingsDto>>> GetGlobalSettings(CancellationToken cancellationToken)
    {
        var settings = await _aiSettingsService.GetGlobalSettingsAsync(cancellationToken);
        return Ok(ApiResponse<AISettingsDto>.Ok(settings));
    }

    [HttpPut]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<AISettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AISettingsDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AISettingsDto>>> UpdateGlobalSettings(
        UpdateAISettingsDto request,
        CancellationToken cancellationToken)
    {
        var settings = await _aiSettingsService.UpdateGlobalSettingsAsync(request, cancellationToken);
        return Ok(ApiResponse<AISettingsDto>.Ok(settings, "AI settings updated successfully."));
    }

    [HttpGet("agents/{agentId:guid}/resolved")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(ApiResponse<ResolvedAISettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ResolvedAISettingsDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ResolvedAISettingsDto>>> GetResolvedAgentSettings(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var settings = await _aiSettingsService.GetResolvedSettingsAsync(agentId, cancellationToken);
        if (settings is null)
        {
            return NotFound(ApiResponse<ResolvedAISettingsDto>.Fail("Agent was not found."));
        }

        return Ok(ApiResponse<ResolvedAISettingsDto>.Ok(settings));
    }

    [HttpGet("models")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "provider", "freeOnly" })]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LLMModelDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LLMModelDto>>>> GetModels(
        [FromQuery] string provider,
        [FromQuery] bool freeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var models = GetStaticModels(provider, freeOnly);
        if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                models = await GetOpenRouterModelsAsync(freeOnly, cancellationToken);
            }
            catch (HttpRequestException)
            {
                models = GetStaticModels(provider, freeOnly);
            }
            catch (TaskCanceledException)
            {
                models = GetStaticModels(provider, freeOnly);
            }
        }

        return Ok(ApiResponse<IReadOnlyList<LLMModelDto>>.Ok(models));
    }

    [HttpPost("test")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<TestLLMProviderResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TestLLMProviderResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TestLLMProviderResultDto>>> TestProvider(
        TestLLMProviderDto request,
        CancellationToken cancellationToken)
    {
        var chatRequest = await _aiSettingsService.BuildChatRequestAsync(
            request.AgentId,
            request.Prompt,
            cancellationToken);

        if (chatRequest is null)
        {
            return NotFound(ApiResponse<TestLLMProviderResultDto>.Fail("Agent was not found."));
        }

        var provider = _llmProviderFactory.GetProvider(chatRequest.Provider);
        var response = await provider.ChatAsync(chatRequest, cancellationToken);

        return Ok(ApiResponse<TestLLMProviderResultDto>.Ok(new TestLLMProviderResultDto
        {
            Provider = chatRequest.Provider,
            Model = chatRequest.Model,
            Content = response.Content,
            RawResponseJson = response.RawResponseJson
        }, "LLM provider test completed successfully."));
    }

    [HttpPost("chat")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
    [ProducesResponseType(typeof(ApiResponse<TestLLMProviderResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TestLLMProviderResultDto>>> Chat(
        DirectChatDto request,
        CancellationToken cancellationToken)
    {
        var chatRequest = await _aiSettingsService.BuildDirectChatRequestAsync(request, cancellationToken);
        var provider = _llmProviderFactory.GetProvider(chatRequest.Provider);
        var response = await provider.ChatAsync(chatRequest, cancellationToken);

        return Ok(ApiResponse<TestLLMProviderResultDto>.Ok(new TestLLMProviderResultDto
        {
            Provider = chatRequest.Provider,
            Model = chatRequest.Model,
            Content = response.Content,
            RawResponseJson = response.RawResponseJson
        }, "Chat completed successfully."));
    }

    private async Task<IReadOnlyList<LLMModelDto>> GetOpenRouterModelsAsync(
        bool freeOnly,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClientFactory
            .CreateClient("llm")
            .GetAsync("https://openrouter.ai/api/v1/models", cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<LLMModelDto>();
        }

        var models = new List<LLMModelDto>();
        foreach (var item in data.EnumerateArray())
        {
            var id = ReadString(item, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var pricing = item.TryGetProperty("pricing", out var pricingElement)
                ? pricingElement
                : default;

            var promptPrice = ReadDecimal(pricing, "prompt");
            var completionPrice = ReadDecimal(pricing, "completion");
            var isFree = promptPrice == 0 && completionPrice == 0;

            if ((freeOnly && !isFree) || !IsTextModel(item))
            {
                continue;
            }

            models.Add(new LLMModelDto
            {
                Id = id,
                Name = ReadString(item, "name") ?? id,
                Provider = "OpenRouter",
                Description = ReadString(item, "description"),
                ContextLength = ReadInt(item, "context_length"),
                PromptPrice = promptPrice,
                CompletionPrice = completionPrice,
                IsFree = isFree
            });
        }

        return models
            .OrderByDescending(model => model.Id.Contains(":free", StringComparison.OrdinalIgnoreCase))
            .ThenBy(model => model.Name)
            .ToArray();
    }

    private static IReadOnlyList<LLMModelDto> GetStaticModels(string provider, bool freeOnly)
    {
        var normalizedProvider = provider.Trim();
        var models = normalizedProvider.ToLowerInvariant() switch
        {
            "gemini" => new[]
            {
                StaticModel("Gemini", "gemini-2.5-flash", "Gemini 2.5 Flash"),
                StaticModel("Gemini", "gemini-2.5-pro", "Gemini 2.5 Pro"),
                StaticModel("Gemini", "gemini-2.0-flash", "Gemini 2.0 Flash"),
                StaticModel("Gemini", "gemini-2.0-flash-lite", "Gemini 2.0 Flash Lite")
            },
            "ollama" => new[]
            {
                StaticModel("Ollama", "llama3.1", "Llama 3.1"),
                StaticModel("Ollama", "llama3.2", "Llama 3.2"),
                StaticModel("Ollama", "qwen2.5", "Qwen 2.5"),
                StaticModel("Ollama", "mistral", "Mistral"),
                StaticModel("Ollama", "deepseek-r1", "DeepSeek R1")
            },
            "openrouter" => new[]
            {
                StaticModel("OpenRouter", "openrouter/free", "Free Models Router")
            },
            _ => Array.Empty<LLMModelDto>()
        };

        return freeOnly ? models.Where(model => model.IsFree).ToArray() : models;
    }

    private static LLMModelDto StaticModel(string provider, string id, string name)
    {
        return new LLMModelDto
        {
            Id = id,
            Name = name,
            Provider = provider,
            IsFree = true
        };
    }

    private static string? ReadString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
            ? number
            : null;
    }

    private static decimal ReadDecimal(JsonElement item, string propertyName)
    {
        if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
            _ => 0
        };
    }

    private static bool IsTextModel(JsonElement item)
    {
        if (!item.TryGetProperty("architecture", out var architecture))
        {
            return true;
        }

        return HasModality(architecture, "input_modalities", "text")
            && HasModality(architecture, "output_modalities", "text");
    }

    private static bool HasModality(JsonElement architecture, string propertyName, string modality)
    {
        if (!architecture.TryGetProperty(propertyName, out var modalities) || modalities.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        return modalities
            .EnumerateArray()
            .Any(value => value.ValueKind == JsonValueKind.String
                && value.GetString()?.Equals(modality, StringComparison.OrdinalIgnoreCase) == true);
    }
}
