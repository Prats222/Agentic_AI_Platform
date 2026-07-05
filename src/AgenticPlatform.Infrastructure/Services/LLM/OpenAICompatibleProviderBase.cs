using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.LLM;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public abstract class OpenAICompatibleProviderBase : ILLMProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _providerName;
    private readonly string _defaultBaseUrl;

    protected OpenAICompatibleProviderBase(IHttpClientFactory httpClientFactory, string providerName, string defaultBaseUrl)
    {
        _httpClientFactory = httpClientFactory;
        _providerName = providerName;
        _defaultBaseUrl = defaultBaseUrl;
    }

    public abstract AIProvider Provider { get; }

    public async Task<LLMChatResponse> ChatAsync(LLMChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException($"{_providerName} requires an API key.");
        }

        var endpoint = BuildEndpoint(request.BaseUrl, _defaultBaseUrl, "chat/completions");
        var payload = new
        {
            model = request.Model,
            messages = BuildMessages(request),
            temperature = request.Temperature,
            top_p = request.TopP,
            max_tokens = request.MaxTokens
        };

        var client = _httpClientFactory.CreateClient("llm");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new LLMProviderException(_providerName, response.StatusCode, raw);
        }

        using var document = JsonDocument.Parse(raw);
        var content = document.RootElement
            .GetProperty("choices")[0]
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
        if (normalizedBaseUrl.EndsWith(path, StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(normalizedBaseUrl);
        }

        return new Uri(new Uri(normalizedBaseUrl.TrimEnd('/') + "/"), path);
    }
}
