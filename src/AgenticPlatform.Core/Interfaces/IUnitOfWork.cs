using AgenticPlatform.Core.Entities;

namespace AgenticPlatform.Core.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IAgentRepository Agents { get; }
    IWorkflowRepository Workflows { get; }
    IToolRepository Tools { get; }
    IExecutionRepository Executions { get; }

    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
