using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            var results = new List<object>();
            var abstractText = root.TryGetProperty("AbstractText", out var abstractElement)
                ? abstractElement.GetString()
                : string.Empty;
            var heading = root.TryGetProperty("Heading", out var headingElement)
                ? headingElement.GetString()
                : string.Empty;
            var sourceUrl = root.TryGetProperty("AbstractURL", out var urlElement)
                ? urlElement.GetString()
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(abstractText) || !string.IsNullOrWhiteSpace(sourceUrl))
            {
                results.Add(new
                {
                    title = heading,
                    snippet = abstractText,
                    url = sourceUrl
                });
            }

            if (root.TryGetProperty("RelatedTopics", out var relatedTopics) && relatedTopics.ValueKind == JsonValueKind.Array)
            {
                foreach (var topic in relatedTopics.EnumerateArray().Take(6))
                {
                    AddRelatedTopic(results, topic);
                }
            }

            if (results.Count == 0)
            {
                results.AddRange(await SearchDuckDuckGoHtmlAsync(client, query, cancellationToken));
            }

            return JsonSerializer.Serialize(new
            {
                query,
                heading,
                abstractText,
                sourceUrl,
                results,
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

    private static void AddRelatedTopic(List<object> results, JsonElement topic)
    {
        if (topic.TryGetProperty("Topics", out var nested) && nested.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in nested.EnumerateArray().Take(3))
            {
                AddRelatedTopic(results, child);
            }

            return;
        }

        var text = topic.TryGetProperty("Text", out var textElement) ? textElement.GetString() : null;
        var url = topic.TryGetProperty("FirstURL", out var urlElement) ? urlElement.GetString() : null;
        if (!string.IsNullOrWhiteSpace(text))
        {
            results.Add(new
            {
                title = text.Split(" - ")[0],
                snippet = text,
                url
            });
        }
    }

    private static async Task<IReadOnlyList<object>> SearchDuckDuckGoHtmlAsync(HttpClient client, string query, CancellationToken cancellationToken)
    {
        var html = await client.GetStringAsync($"https://duckduckgo.com/html/?q={Uri.EscapeDataString(query)}", cancellationToken);
        var matches = Regex.Matches(
            html,
            "<a[^>]+class=\"result__a\"[^>]+href=\"(?<url>[^\"]+)\"[^>]*>(?<title>.*?)</a>.*?<a[^>]+class=\"result__snippet\"[^>]*>(?<snippet>.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return matches
            .Take(5)
            .Select(match => (object)new
            {
                title = CleanHtml(match.Groups["title"].Value),
                snippet = CleanHtml(match.Groups["snippet"].Value),
                url = WebUtility.HtmlDecode(match.Groups["url"].Value)
            })
            .ToArray();
    }

    private static string CleanHtml(string value)
    {
        var noTags = Regex.Replace(value, "<.*?>", string.Empty, RegexOptions.Singleline);
        return WebUtility.HtmlDecode(noTags).Trim();
    }
}
