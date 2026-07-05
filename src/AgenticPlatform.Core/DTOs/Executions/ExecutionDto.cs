using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Executions;

public sealed class ExecutionDto
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public ExecutionTargetType TargetType { get; set; }
    public ExecutionStatus Status { get; set; }
    public Guid? AgentId { get; set; }
    public Guid? WorkflowId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string InputJson { get; set; } = "{}";
    public string? OutputJson { get; set; }
    public string? ErrorMessage { get; set; }
    public double? DurationMs { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public int? EstimatedInputTokens { get; set; }
    public int? EstimatedOutputTokens { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public IReadOnlyCollection<ExecutionLogDto> Logs { get; set; } = Array.Empty<ExecutionLogDto>();
}
