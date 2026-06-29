using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Core.Interfaces;

public interface IToolExecutionService
{
    Task<ToolExecutionResult?> ExecuteAsync(Guid toolId, string inputJson, CancellationToken cancellationToken = default);
}
