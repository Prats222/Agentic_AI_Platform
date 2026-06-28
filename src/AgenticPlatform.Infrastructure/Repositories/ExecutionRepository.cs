using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Repositories;

public sealed class ExecutionRepository : Repository<Execution>, IExecutionRepository
{
    public ExecutionRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Execution?> GetWithLogsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(execution => execution.Agent)
            .Include(execution => execution.Workflow)
            .Include(execution => execution.Logs.OrderBy(log => log.CreatedAt))
            .FirstOrDefaultAsync(execution => execution.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Execution>> GetByStatusAsync(
        ExecutionStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(execution => execution.Status == status)
            .OrderByDescending(execution => execution.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Execution>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(execution => execution.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
