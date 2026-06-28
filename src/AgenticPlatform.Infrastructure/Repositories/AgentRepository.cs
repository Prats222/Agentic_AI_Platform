using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Repositories;

public sealed class AgentRepository : Repository<Agent>, IAgentRepository
{
    public AgentRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Agent?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(agent => agent.Name == name, cancellationToken);
    }

    public async Task<Agent?> GetWithToolsAndWorkflowsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(agent => agent.Tools)
            .Include(agent => agent.Workflows)
            .FirstOrDefaultAsync(agent => agent.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Agent>> GetByStatusAsync(
        AgentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(agent => agent.Status == status)
            .OrderBy(agent => agent.Name)
            .ToListAsync(cancellationToken);
    }
}
