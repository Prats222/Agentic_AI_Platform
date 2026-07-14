using System.Net;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Search;
using Microsoft.Extensions.Logging;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class WebSearchService : IWebSearchService
{
    private static readonly string[] SearchSignals =
    [
        "today", "latest", "current", "weather", "temperature", "forecast", "score", "news", "live", "now",
        "yesterday", "this week", "search the web", "look up", "online"
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebSearchService> logger)
    {
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
        if (IsWeatherQuery(query))
        {
            var weatherResult = await TryGetWeatherAsync(query, cancellationToken);
            if (weatherResult is not null)
            {
                return weatherResult;
            }
        }

        return await TrySearchWithDuckDuckGoAsync(query, cancellationToken);
    }

    private async Task<WebSearchResult?> TryGetWeatherAsync(string query, CancellationToken cancellationToken)
    {
        var locationQuery = NormalizeLocationQuery(ExtractWeatherLocation(query));
        if (string.IsNullOrWhiteSpace(locationQuery))
        {
            return null;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            var client = _httpClientFactory.CreateClient("live-search");
            var geocodingUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(locationQuery)}&count=1&language=en&format=json";
            using var geocodingResponse = await client.GetAsync(geocodingUrl, timeout.Token);
            if (!geocodingResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo geocoding returned {StatusCode} for {Location}.", geocodingResponse.StatusCode, locationQuery);
                return null;
            }

            using var geocodingDocument = JsonDocument.Parse(await geocodingResponse.Content.ReadAsStringAsync(timeout.Token));
            if (!geocodingDocument.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return null;
            }

            var location = results[0];
            var latitude = location.GetProperty("latitude").GetDouble();
            var longitude = location.GetProperty("longitude").GetDouble();
            var name = location.GetProperty("name").GetString() ?? locationQuery;
            var region = GetOptionalString(location, "admin1");
            var country = GetOptionalString(location, "country");
            var displayLocation = string.Join(", ", new[] { name, region, country }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct());

            var forecastUrl = "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                "&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
                "&forecast_days=2&timezone=auto";
            using var forecastResponse = await client.GetAsync(forecastUrl, timeout.Token);
            if (!forecastResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo forecast returned {StatusCode} for {Location}.", forecastResponse.StatusCode, displayLocation);
                return null;
            }

            using var forecastDocument = JsonDocument.Parse(await forecastResponse.Content.ReadAsStringAsync(timeout.Token));
            var root = forecastDocument.RootElement;
            var current = root.GetProperty("current");
            var units = root.GetProperty("current_units");
            var daily = root.GetProperty("daily");
            var dailyUnits = root.GetProperty("daily_units");
            var currentCode = current.GetProperty("weather_code").GetInt32();
            var todayCode = daily.GetProperty("weather_code")[0].GetInt32();
            var observedAt = current.GetProperty("time").GetString();

            var context = $"""
                Verified current weather for {displayLocation}, observed at {observedAt} local time:
                - Conditions: {DescribeWeatherCode(currentCode)}
                - Temperature: {FormatNumber(current, "temperature_2m")} {GetOptionalString(units, "temperature_2m")}
                - Feels like: {FormatNumber(current, "apparent_temperature")} {GetOptionalString(units, "apparent_temperature")}
                - Relative humidity: {FormatNumber(current, "relative_humidity_2m")} {GetOptionalString(units, "relative_humidity_2m")}
                - Precipitation now: {FormatNumber(current, "precipitation")} {GetOptionalString(units, "precipitation")}
                - Wind speed: {FormatNumber(current, "wind_speed_10m")} {GetOptionalString(units, "wind_speed_10m")}
                Today's forecast: {DescribeWeatherCode(todayCode)}, low {FormatArrayNumber(daily, "temperature_2m_min", 0)} {GetOptionalString(dailyUnits, "temperature_2m_min")}, high {FormatArrayNumber(daily, "temperature_2m_max", 0)} {GetOptionalString(dailyUnits, "temperature_2m_max")}, maximum precipitation probability {FormatArrayNumber(daily, "precipitation_probability_max", 0)} {GetOptionalString(dailyUnits, "precipitation_probability_max")}.
                Data source: Open-Meteo weather forecast API. Forecast data may differ slightly from physical station observations.
                """;

            return new WebSearchResult
            {
                Provider = "Open-Meteo",
                Context = context,
                Sources =
                [
                    new WebSearchSource
                    {
                        Title = $"Open-Meteo forecast for {displayLocation}",
                        Url = forecastUrl,
                        Snippet = $"{DescribeWeatherCode(currentCode)}, {FormatNumber(current, "temperature_2m")} {GetOptionalString(units, "temperature_2m")}."
                    }
                ]
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _logger.LogWarning(exception, "Open-Meteo weather lookup failed for query {Query}.", query);
            return null;
        }
    }

    private async Task<WebSearchResult?> TrySearchWithDuckDuckGoAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var client = _httpClientFactory.CreateClient("live-search");
            var html = await GetSearchHtmlAsync(client, "https://html.duckduckgo.com/html/", query, timeout.Token);
            var matches = Regex.Matches(
                html,
                "<a[^>]+class=\"result__a\"[^>]+href=\"(?<url>[^\"]+)\"[^>]*>(?<title>.*?)</a>.*?<a[^>]+class=\"result__snippet\"[^>]*>(?<snippet>.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var sources = CreateSources(matches);
            if (sources.Length == 0)
            {
                html = await GetSearchHtmlAsync(client, "https://lite.duckduckgo.com/lite/", query, timeout.Token);
                matches = Regex.Matches(
                    html,
                    "<a[^>]+href=['\"](?<url>[^'\"]+)['\"][^>]+class=['\"]result-link['\"][^>]*>(?<title>.*?)</a>.*?<td[^>]+class=['\"]result-snippet['\"][^>]*>(?<snippet>.*?)</td>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                sources = CreateSources(matches);
            }

            if (sources.Length == 0)
            {
                _logger.LogWarning("DuckDuckGo returned no parseable results for query {Query}.", query);
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
            _logger.LogWarning(exception, "DuckDuckGo fallback search was unavailable for query {Query}.", query);
            return null;
        }
    }

    private static async Task<string> GetSearchHtmlAsync(
        HttpClient client,
        string endpoint,
        string query,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}?q={Uri.EscapeDataString(query)}");
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static WebSearchSource[] CreateSources(MatchCollection matches)
    {
        return matches
            .Take(5)
            .Select(match => new WebSearchSource
            {
                Title = CleanHtml(match.Groups["title"].Value),
                Url = NormalizeDuckDuckGoUrl(WebUtility.HtmlDecode(match.Groups["url"].Value)),
                Snippet = CleanHtml(match.Groups["snippet"].Value)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
            .ToArray();
    }

    private static bool IsWeatherQuery(string query)
    {
        return Regex.IsMatch(query, "\\b(weather|temperature|forecast|rain|raining)\\b", RegexOptions.IgnoreCase);
    }

    private static string ExtractWeatherLocation(string query)
    {
        var afterPreposition = Regex.Match(
            query,
            "\\b(?:in|for|at)\\s+(?<location>[\\p{L}\\p{N}\\s,.'-]+?)(?=\\s+(?:today|tomorrow|tonight|right now|now|currently|this week)\\b|[?!.]|$)",
            RegexOptions.IgnoreCase);
        if (afterPreposition.Success)
        {
            return afterPreposition.Groups["location"].Value.Trim();
        }

        var cleaned = Regex.Replace(
            query,
            "\\b(?:what|what's|is|the|weather|temperature|forecast|rain|raining|today|tomorrow|tonight|currently|current|now|right|like|how|will|be)\\b",
            " ",
            RegexOptions.IgnoreCase);
        return Regex.Replace(cleaned, "\\s+", " ").Trim(" ,?.!".ToCharArray());
    }

    private static string NormalizeLocationQuery(string location)
    {
        var city = location.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? location;
        return city.ToLowerInvariant() switch
        {
            "bangalore" => "Bengaluru",
            "bombay" => "Mumbai",
            "calcutta" => "Kolkata",
            "madras" => "Chennai",
            _ => city
        };
    }

    private static string DescribeWeatherCode(int code)
    {
        return code switch
        {
            0 => "clear sky",
            1 => "mainly clear",
            2 => "partly cloudy",
            3 => "overcast",
            45 or 48 => "foggy",
            51 or 53 or 55 => "drizzle",
            56 or 57 => "freezing drizzle",
            61 or 63 or 65 => "rain",
            66 or 67 => "freezing rain",
            71 or 73 or 75 or 77 => "snow",
            80 or 81 or 82 => "rain showers",
            85 or 86 => "snow showers",
            95 => "thunderstorms",
            96 or 99 => "thunderstorms with hail",
            _ => "unclassified conditions"
        };
    }

    private static string FormatNumber(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetDouble().ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static string FormatArrayNumber(JsonElement element, string propertyName, int index)
    {
        var value = element.GetProperty(propertyName)[index];
        return value.ValueKind == JsonValueKind.Null
            ? "unavailable"
            : value.GetDouble().ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static string GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string CleanHtml(string value)
    {
        return WebUtility.HtmlDecode(Regex.Replace(value, "<.*?>", string.Empty, RegexOptions.Singleline)).Trim();
    }

    private static string NormalizeDuckDuckGoUrl(string value)
    {
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            value = $"https:{value}";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["uddg"] ?? value;
    }
}
