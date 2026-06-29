using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Core.Interfaces;

public interface IToolExecutor
{
    string Name { get; }
    bool CanExecute(Tool tool);
    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken = default);
}
