using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class ExecutionLog : BaseEntity
{
    public Guid ExecutionId { get; set; }
    public Execution Execution { get; set; } = null!;

    public ExecutionLogLevel Level { get; set; } = ExecutionLogLevel.Information;
    public string Message { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
}
