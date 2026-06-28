using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Repositories;

public sealed class WorkflowRepository : Repository<Workflow>, IWorkflowRepository
{
    public WorkflowRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Workflow?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(workflow => workflow.Name == name, cancellationToken);
    }

    public async Task<Workflow?> GetWithStepsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(workflow => workflow.Steps.OrderBy(step => step.Order))
            .ThenInclude(step => step.Tool)
            .Include(workflow => workflow.Steps.OrderBy(step => step.Order))
            .ThenInclude(step => step.Agent)
            .FirstOrDefaultAsync(workflow => workflow.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Workflow>> GetByStatusAsync(
        WorkflowStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(workflow => workflow.Status == status)
            .OrderBy(workflow => workflow.Name)
            .ToListAsync(cancellationToken);
    }
}
