namespace AgenticPlatform.Core.Entities;

public sealed class HumanApprovalRequest : BaseEntity
{
    public Guid ExecutionId { get; set; }
    public Execution Execution { get; set; } = null!;

    public Guid WorkflowStepId { get; set; }
    public WorkflowStep WorkflowStep { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public string? ReviewerComment { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
