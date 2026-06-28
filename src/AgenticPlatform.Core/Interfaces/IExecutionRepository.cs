using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Interfaces;

public interface IExecutionRepository : IRepository<Execution>
{
    Task<Execution?> GetWithLogsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Execution>> GetByStatusAsync(ExecutionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Execution>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
