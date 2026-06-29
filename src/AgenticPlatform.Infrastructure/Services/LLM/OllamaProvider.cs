using System.Net.Http.Json;
using System.Text.Json;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.LLM;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public sealed class OllamaProvider : ILLMProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public AIProvider Provider => AIProvider.Ollama;

    public async Task<LLMChatResponse> ChatAsync(LLMChatRequest request, CancellationToken cancellationToken = default)
    {
        var endpoint = BuildEndpoint(request.BaseUrl, "http://localhost:11434", "api/chat");
        var messages = BuildMessages(request);
        var payload = new
        {
            model = request.Model,
            messages,
            stream = false,
            options = new
            {
                temperature = request.Temperature,
                top_p = request.TopP,
                num_predict = request.MaxTokens
            }
        };

        var client = _httpClientFactory.CreateClient("llm");
        using var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new LLMProviderException("Ollama", response.StatusCode, raw);
        }

        using var document = JsonDocument.Parse(raw);
        var content = document.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return new LLMChatResponse
        {
            Content = content,
            RawResponseJson = raw
        };
    }

    private static object[] BuildMessages(LLMChatRequest request)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }

        messages.AddRange(request.Messages.Select(message => new
        {
            role = message.Role,
            content = message.Content
        }));

        return messages.ToArray();
    }

    private static Uri BuildEndpoint(string? baseUrl, string fallbackBaseUrl, string path)
    {
        var normalizedBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? fallbackBaseUrl : baseUrl.Trim();
        return new Uri(new Uri(normalizedBaseUrl.TrimEnd('/') + "/"), path);
    }
}
