using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Executions;

public sealed class CreateExecutionDto
{
    public ExecutionTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string InputJson { get; set; } = "{}";
}
