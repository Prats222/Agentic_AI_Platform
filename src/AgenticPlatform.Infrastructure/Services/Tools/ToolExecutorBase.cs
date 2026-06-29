using System.Diagnostics;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public abstract class ToolExecutorBase
{
    protected async Task<ToolExecutionResult> ExecuteCoreAsync(
        Tool tool,
        Func<Task<string>> executeAsync)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var resultJson = await executeAsync();
            stopwatch.Stop();

            return new ToolExecutionResult
            {
                ToolId = tool.Id,
                ToolName = tool.Name,
                ExecutorName = GetType().Name,
                Succeeded = true,
                ResultJson = resultJson,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new ToolExecutionResult
            {
                ToolId = tool.Id,
                ToolName = tool.Name,
                ExecutorName = GetType().Name,
                Succeeded = false,
                ResultJson = "{}",
                ErrorMessage = ex.Message,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                DurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }
}
