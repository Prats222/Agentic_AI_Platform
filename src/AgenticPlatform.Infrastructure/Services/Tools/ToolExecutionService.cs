using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class ToolExecutionService : IToolExecutionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<IToolExecutor> _toolExecutors;

    public ToolExecutionService(ApplicationDbContext dbContext, IEnumerable<IToolExecutor> toolExecutors)
    {
        _dbContext = dbContext;
        _toolExecutors = toolExecutors;
    }

    public async Task<ToolExecutionResult?> ExecuteAsync(Guid toolId, string inputJson, CancellationToken cancellationToken = default)
    {
        var tool = await _dbContext.Tools
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == toolId, cancellationToken);

        if (tool is null)
        {
            return null;
        }

        if (!tool.IsEnabled)
        {
            throw new InvalidOperationException("Tool is disabled.");
        }

        var executor = _toolExecutors.FirstOrDefault(item => item.CanExecute(tool));
        if (executor is null)
        {
            throw new NotSupportedException($"No executor is registered for tool category '{tool.Category}'.");
        }

        return await executor.ExecuteAsync(new ToolExecutionRequest
        {
            Tool = tool,
            InputJson = inputJson
        }, cancellationToken);
    }
}
