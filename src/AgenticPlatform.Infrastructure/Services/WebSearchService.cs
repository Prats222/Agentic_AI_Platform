using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Search;
using AgenticPlatform.Infrastructure.Data;
using AgenticPlatform.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class WebSearchService : IWebSearchService
{
    private static readonly string[] SearchSignals =
    [
        "today", "latest", "current", "weather", "score", "news", "live", "now",
        "yesterday", "this week", "search the web", "look up", "online"
    ];

    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<WebSearchService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool ShouldSearch(string prompt)
    {
        var text = prompt.ToLowerInvariant();
        return SearchSignals.Any(text.Contains);
    }

    public async Task<WebSearchResult?> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var groqResult = await TrySearchWithGroqAsync(query, cancellationToken);
        if (groqResult is not null)
        {
            return groqResult;
        }

        return await TrySearchWithDuckDuckGoAsync(query, cancellationToken);
    }

    private async Task<WebSearchResult?> TrySearchWithGroqAsync(string query, CancellationToken cancellationToken)
    {
        var settings = await _dbContext.AISettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == AISettingsConfiguration.GlobalSettingsId, cancellationToken);
        var apiKey = !string.IsNullOrWhiteSpace(settings?.GroqApiKey)
            ? settings.GroqApiKey
            : settings?.Provider == AgenticPlatform.Core.Enums.AIProvider.Groq ? settings.ApiKey : null;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = JsonContent.Create(new
                {
                    model = "groq/compound-mini",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $"Search the live web and answer this query with current facts and source URLs: {query}"
                        }
                    }
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("Groq-Model-Version", "latest");

            using var response = await _httpClientFactory.CreateClient("live-search").SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Groq live search returned {StatusCode}; falling back.", response.StatusCode);
                return null;
            }

            var raw = await response.Content.ReadAsStringAsync(timeout.Token);
            using var document = JsonDocument.Parse(raw);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            return new WebSearchResult
            {
                Provider = "Groq Compound Mini",
                Context = content
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _logger.LogWarning(exception, "Groq live search was unavailable; using the fallback search provider.");
            return null;
        }
    }

    private async Task<WebSearchResult?> TrySearchWithDuckDuckGoAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(7));
            var client = _httpClientFactory.CreateClient("live-search");
            var html = await client.GetStringAsync(
                $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}",
                timeout.Token);
            var matches = Regex.Matches(
                html,
                "<a[^>]+class=\"result__a\"[^>]+href=\"(?<url>[^\"]+)\"[^>]*>(?<title>.*?)</a>.*?<a[^>]+class=\"result__snippet\"[^>]*>(?<snippet>.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var sources = matches
                .Take(5)
                .Select(match => new WebSearchSource
                {
                    Title = CleanHtml(match.Groups["title"].Value),
                    Url = NormalizeDuckDuckGoUrl(WebUtility.HtmlDecode(match.Groups["url"].Value)),
                    Snippet = CleanHtml(match.Groups["snippet"].Value)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Title))
                .ToArray();
            if (sources.Length == 0)
            {
                return null;
            }

            return new WebSearchResult
            {
                Provider = "DuckDuckGo",
                Sources = sources,
                Context = string.Join(
                    Environment.NewLine,
                    sources.Select((item, index) => $"[{index + 1}] {item.Title}: {item.Snippet} Source: {item.Url}"))
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "DuckDuckGo fallback search was unavailable.");
            return null;
        }
    }

    private static string CleanHtml(string value)
    {
        return WebUtility.HtmlDecode(Regex.Replace(value, "<.*?>", string.Empty, RegexOptions.Singleline)).Trim();
    }

    private static string NormalizeDuckDuckGoUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["uddg"] ?? value;
    }
}
