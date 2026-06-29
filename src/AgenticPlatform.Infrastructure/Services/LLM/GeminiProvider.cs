using System.Net.Http.Json;
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
        var payload = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = request.SystemPrompt }
                }
            },
            contents = request.Messages.Select(message => new
            {
                role = message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user",
                parts = new[]
                {
                    new { text = message.Content }
                }
            }),
            generationConfig = new
            {
                temperature = request.Temperature,
                topP = request.TopP,
                maxOutputTokens = request.MaxTokens
            }
        };

        var client = _httpClientFactory.CreateClient("llm");
        using var response = await client.PostAsJsonAsync(endpoint, payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new LLMProviderException("Gemini", response.StatusCode, raw);
        }

        using var document = JsonDocument.Parse(raw);
        var content = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return new LLMChatResponse
        {
            Content = content,
            RawResponseJson = raw
        };
    }
}
