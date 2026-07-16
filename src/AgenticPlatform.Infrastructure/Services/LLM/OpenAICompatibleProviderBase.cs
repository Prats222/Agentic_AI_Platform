using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
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
    protected virtual string MaxTokensParameter => "max_tokens";

    public async Task<LLMChatResponse> ChatAsync(LLMChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException($"{_providerName} requires an API key.");
        }

        var endpoint = BuildEndpoint(request.BaseUrl, _defaultBaseUrl, "chat/completions");
        var payload = BuildPayload(request, false);

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
        var content = TryReadCompletedContent(document.RootElement);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new LLMProviderException(
                _providerName,
                System.Net.HttpStatusCode.BadGateway,
                "The provider completed without returning text. Retry the request or choose another model.");
        }

        return new LLMChatResponse
        {
            Content = content,
            RawResponseJson = raw
        };
    }

    public async IAsyncEnumerable<LLMStreamChunk> StreamChatAsync(
        LLMChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException($"{_providerName} requires an API key.");
        }

        var endpoint = BuildEndpoint(request.BaseUrl, _defaultBaseUrl, "chat/completions");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(BuildPayload(request, true))
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

        var client = _httpClientFactory.CreateClient("llm");
        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LLMProviderException(_providerName, response.StatusCode, error);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                yield break;
            }

            using var document = JsonDocument.Parse(data);
            var content = TryReadDeltaContent(document.RootElement);
            if (!string.IsNullOrEmpty(content))
            {
                yield return new LLMStreamChunk { Content = content };
            }
        }
    }

    private object BuildPayload(LLMChatRequest request, bool stream)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["messages"] = BuildMessages(request),
            ["temperature"] = request.Temperature,
            ["top_p"] = request.TopP,
            [MaxTokensParameter] = request.MaxTokens,
            ["stream"] = stream
        };
    }

    private static string TryReadCompletedContent(JsonElement root)
    {
        return root.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0
            && choices[0].TryGetProperty("message", out var message)
            && message.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.String
                ? content.GetString() ?? string.Empty
                : string.Empty;
    }

    private static string TryReadDeltaContent(JsonElement root)
    {
        return root.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0
            && choices[0].TryGetProperty("delta", out var delta)
            && delta.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.String
                ? content.GetString() ?? string.Empty
                : string.Empty;
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
