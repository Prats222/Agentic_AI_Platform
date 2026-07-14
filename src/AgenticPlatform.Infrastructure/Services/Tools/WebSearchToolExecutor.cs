using System.Text.Json;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class WebSearchToolExecutor : ToolExecutorBase, IToolExecutor
{
    private readonly IWebSearchService _webSearchService;

    public WebSearchToolExecutor(IWebSearchService webSearchService)
    {
        _webSearchService = webSearchService;
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

            var searchResult = await _webSearchService.SearchAsync(query, cancellationToken)
                ?? throw new InvalidOperationException("Live web search is temporarily unavailable. Please retry shortly.");

            return JsonSerializer.Serialize(new
            {
                query,
                provider = searchResult.Provider,
                context = searchResult.Context,
                results = searchResult.Sources
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
