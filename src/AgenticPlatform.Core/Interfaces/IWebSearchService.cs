using AgenticPlatform.Core.Models.Search;

namespace AgenticPlatform.Core.Interfaces;

public interface IWebSearchService
{
    bool ShouldSearch(string prompt);
    Task<WebSearchResult?> SearchAsync(string query, CancellationToken cancellationToken = default);
}
