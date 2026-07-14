namespace AgenticPlatform.Core.Models.Search;

public sealed class WebSearchResult
{
    public string Context { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public IReadOnlyCollection<WebSearchSource> Sources { get; set; } = Array.Empty<WebSearchSource>();
}

public sealed class WebSearchSource
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Snippet { get; set; }
}
