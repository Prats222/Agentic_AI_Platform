using System.Text.Json;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class WebSearchToolExecutor : ToolExecutorBase, IToolExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebSearchToolExecutor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string Name => BuiltInToolCategories.WebSearch;

    public bool CanExecute(Tool tool)
    {
        return tool.Category.Equals(BuiltInToolCategories.WebSearch, StringComparison.OrdinalIgnoreCase);
    }

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(request.Tool, async () =>
        {
            using var input = JsonDocument.Parse(request.InputJson);
            var query = ReadInputValue(input.RootElement, "query")
                ?? ReadInputValue(input.RootElement, "prompt")
                ?? ReadInputValue(input.RootElement, "input");

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new InvalidOperationException("Web search input requires a 'query' value.");
            }

            var endpoint = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_redirect=1&no_html=1";
            var client = _httpClientFactory.CreateClient("tool-runner");
            var raw = await client.GetStringAsync(endpoint, cancellationToken);

            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            var abstractText = root.TryGetProperty("AbstractText", out var abstractElement)
                ? abstractElement.GetString()
                : string.Empty;
            var heading = root.TryGetProperty("Heading", out var headingElement)
                ? headingElement.GetString()
                : string.Empty;
            var sourceUrl = root.TryGetProperty("AbstractURL", out var urlElement)
                ? urlElement.GetString()
                : string.Empty;

            return JsonSerializer.Serialize(new
            {
                query,
                heading,
                abstractText,
                sourceUrl,
                raw
            });
        });
    }

    private static string? ReadInputValue(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }
}
