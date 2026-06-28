using AgenticPlatform.Core.Entities;

namespace AgenticPlatform.Core.Interfaces;

public interface IToolRepository : IRepository<Tool>
{
    Task<Tool?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tool>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tool>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
