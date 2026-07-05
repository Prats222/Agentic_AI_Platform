namespace AgenticPlatform.Core.DTOs.HumanApprovals;

public sealed class HumanApprovalRequestDto
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public Guid WorkflowStepId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public string? ReviewerComment { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
