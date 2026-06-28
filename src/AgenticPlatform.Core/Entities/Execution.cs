using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class Execution : BaseEntity
{
    public ExecutionTargetType TargetType { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }

    public Guid? WorkflowId { get; set; }
    public Workflow? Workflow { get; set; }

    public Guid? TriggeredByUserId { get; set; }
    public string InputJson { get; set; } = "{}";
    public string? OutputJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ExecutionLog> Logs { get; set; } = new List<ExecutionLog>();
}
