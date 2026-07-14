using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.LLM;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public sealed class GeminiProvider : ILLMProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public AIProvider Provider => AIProvider.Gemini;

    public async Task<LLMChatResponse> ChatAsync(LLMChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException("Gemini requires an API key.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : request.BaseUrl.TrimEnd('/');
        var endpoint = $"{baseUrl}/models/{Uri.EscapeDataString(request.Model)}:generateContent?key={Uri.EscapeDataString(request.ApiKey)}";
        var payload = BuildPayload(request);

        var client = _httpClientFactory.CreateClient("llm");
        using var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new LLMProviderException("Gemini", response.StatusCode, raw);
        }

        using var document = JsonDocument.Parse(raw);
        var content = TryReadContent(document.RootElement);
        if (string.IsNullOrWhiteSpace(content))
        {
            var finishReason = TryReadFinishReason(document.RootElement);
            throw new LLMProviderException(
                "Gemini",
                System.Net.HttpStatusCode.BadGateway,
                $"Gemini completed without text (finish reason: {finishReason}). Retry the request or choose another model.");
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
            throw new InvalidOperationException("Gemini requires an API key.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : request.BaseUrl.TrimEnd('/');
        var endpoint = $"{baseUrl}/models/{Uri.EscapeDataString(request.Model)}:streamGenerateContent?alt=sse&key={Uri.EscapeDataString(request.ApiKey)}";
        var client = _httpClientFactory.CreateClient("llm");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(BuildPayload(request))
        };
        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LLMProviderException("Gemini", response.StatusCode, error);
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

            using var document = JsonDocument.Parse(line[5..].Trim());
            var content = TryReadContent(document.RootElement);
            if (!string.IsNullOrEmpty(content))
            {
                yield return new LLMStreamChunk { Content = content };
            }
        }
    }

    private static object BuildPayload(LLMChatRequest request)
    {
        return new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = request.SystemPrompt } }
            },
            contents = request.Messages.Select(message => new
            {
                role = message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user",
                parts = new[] { new { text = message.Content } }
            }),
            generationConfig = new
            {
                temperature = request.Temperature,
                topP = request.TopP,
                maxOutputTokens = request.MaxTokens
            }
        };
    }

    private static string TryReadContent(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        return parts[0].TryGetProperty("text", out var textElement)
            ? textElement.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string TryReadFinishReason(JsonElement root)
    {
        return root.TryGetProperty("candidates", out var candidates)
            && candidates.ValueKind == JsonValueKind.Array
            && candidates.GetArrayLength() > 0
            && candidates[0].TryGetProperty("finishReason", out var reason)
            ? reason.GetString() ?? "unknown"
            : "unknown";
    }
}
