using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using AgenticPlatform.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace AgenticPlatform.Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        ApplicationDbContext dbContext,
        IAgentRepository agents,
        IWorkflowRepository workflows,
        IToolRepository tools,
        IExecutionRepository executions)
    {
        _dbContext = dbContext;
        Agents = agents;
        Workflows = workflows;
        Tools = tools;
        Executions = executions;
    }

    public IAgentRepository Agents { get; }
    public IWorkflowRepository Workflows { get; }
    public IToolRepository Tools { get; }
    public IExecutionRepository Executions { get; }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var entityType = typeof(T);

        if (_repositories.TryGetValue(entityType, out var repository))
        {
            return (IRepository<T>)repository;
        }

        var newRepository = new Repository<T>(_dbContext);
        _repositories[entityType] = newRepository;

        return newRepository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
        }

        await _dbContext.DisposeAsync();
    }
}
