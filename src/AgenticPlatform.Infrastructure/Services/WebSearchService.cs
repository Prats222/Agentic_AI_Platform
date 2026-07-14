using System.Net;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Search;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<WebSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
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

        var cacheKey = $"weather:{locationQuery.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out WebSearchResult? cachedResult))
        {
            return cachedResult;
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

            var forecastUrl = "https://api.met.no/weatherapi/locationforecast/2.0/compact" +
                $"?lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}";
            using var forecastRequest = new HttpRequestMessage(HttpMethod.Get, forecastUrl);
            forecastRequest.Headers.TryAddWithoutValidation(
                "User-Agent",
                "PratsPilot/1.0 (+https://github.com/Prats222/Agentic_AI_Platform)");
            using var forecastResponse = await client.SendAsync(
                forecastRequest,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (!forecastResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("MET Norway forecast returned {StatusCode} for {Location}.", forecastResponse.StatusCode, displayLocation);
                return null;
            }

            using var forecastDocument = JsonDocument.Parse(await forecastResponse.Content.ReadAsStringAsync(timeout.Token));
            var properties = forecastDocument.RootElement.GetProperty("properties");
            var timeseries = properties.GetProperty("timeseries")
                .EnumerateArray()
                .Select(item => new
                {
                    Element = item.Clone(),
                    Time = DateTimeOffset.Parse(item.GetProperty("time").GetString()!, CultureInfo.InvariantCulture)
                })
                .OrderBy(item => Math.Abs((item.Time - DateTimeOffset.UtcNow).TotalMinutes))
                .ToArray();
            if (timeseries.Length == 0)
            {
                return null;
            }

            var currentPoint = timeseries[0];
            var currentData = currentPoint.Element.GetProperty("data");
            var current = currentData.GetProperty("instant").GetProperty("details");
            var nextHour = currentData.TryGetProperty("next_1_hours", out var nextHourValue)
                ? nextHourValue
                : default;
            var symbolCode = nextHour.ValueKind == JsonValueKind.Object
                && nextHour.TryGetProperty("summary", out var summary)
                ? GetOptionalString(summary, "symbol_code")
                : string.Empty;
            var precipitation = nextHour.ValueKind == JsonValueKind.Object
                && nextHour.TryGetProperty("details", out var precipitationDetails)
                && precipitationDetails.TryGetProperty("precipitation_amount", out var precipitationValue)
                ? precipitationValue.GetDouble()
                : 0;
            var nextDay = timeseries
                .Where(item => item.Time >= currentPoint.Time && item.Time < currentPoint.Time.AddHours(24))
                .Select(item => item.Element.GetProperty("data").GetProperty("instant").GetProperty("details"))
                .ToArray();
            var minimumTemperature = nextDay.Min(item => item.GetProperty("air_temperature").GetDouble());
            var maximumTemperature = nextDay.Max(item => item.GetProperty("air_temperature").GetDouble());

            var context = $"""
                Verified weather forecast for {displayLocation}, valid at {currentPoint.Time:yyyy-MM-dd HH:mm} UTC:
                - Conditions: {DescribeSymbolCode(symbolCode)}
                - Temperature: {FormatNumber(current, "air_temperature")} °C
                - Relative humidity: {FormatNumber(current, "relative_humidity")} %
                - Precipitation in the next hour: {precipitation.ToString("0.#", CultureInfo.InvariantCulture)} mm
                - Wind speed: {FormatNumber(current, "wind_speed")} m/s
                - Cloud cover: {FormatNumber(current, "cloud_area_fraction")} %
                Forecast temperature range for the next 24 hours: {minimumTemperature.ToString("0.#", CultureInfo.InvariantCulture)} °C to {maximumTemperature.ToString("0.#", CultureInfo.InvariantCulture)} °C.
                Data source: MET Norway Locationforecast. This is model forecast data rather than a physical weather-station observation.
                """;

            var result = new WebSearchResult
            {
                Provider = "MET Norway",
                Context = context,
                Sources =
                [
                    new WebSearchSource
                    {
                        Title = $"MET Norway forecast for {displayLocation}",
                        Url = forecastUrl,
                        Snippet = $"{DescribeSymbolCode(symbolCode)}, {FormatNumber(current, "air_temperature")} °C."
                    }
                ]
            };
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _logger.LogWarning(exception, "Weather lookup failed for query {Query}.", query);
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

    private static string DescribeSymbolCode(string code)
    {
        var normalized = Regex.Replace(code, "_(?:day|night|polartwilight)$", string.Empty);
        return normalized switch
        {
            "clearsky" => "clear sky",
            "fair" => "fair",
            "partlycloudy" => "partly cloudy",
            "cloudy" => "cloudy",
            "fog" => "foggy",
            "lightrain" => "light rain",
            "rain" => "rain",
            "heavyrain" => "heavy rain",
            "lightrainshowers" => "light rain showers",
            "rainshowers" => "rain showers",
            "heavyrainshowers" => "heavy rain showers",
            "lightsnow" => "light snow",
            "snow" => "snow",
            "heavysnow" => "heavy snow",
            "sleet" => "sleet",
            "thunderstorm" => "thunderstorms",
            _ => string.IsNullOrWhiteSpace(normalized) ? "conditions unavailable" : normalized.Replace('_', ' ')
        };
    }

    private static string FormatNumber(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetDouble().ToString("0.#", CultureInfo.InvariantCulture);
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
