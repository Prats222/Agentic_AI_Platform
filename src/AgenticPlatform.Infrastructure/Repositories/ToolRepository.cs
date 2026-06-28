using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Repositories;

public sealed class ToolRepository : Repository<Tool>, IToolRepository
{
    public ToolRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Tool?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(tool => tool.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Tool>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(tool => tool.IsEnabled)
            .OrderBy(tool => tool.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tool>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(tool => tool.Category == category)
            .OrderBy(tool => tool.Name)
            .ToListAsync(cancellationToken);
    }
}
