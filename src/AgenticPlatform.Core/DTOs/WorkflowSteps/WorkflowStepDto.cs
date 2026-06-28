using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.WorkflowSteps;

public sealed class WorkflowStepDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public WorkflowStepType StepType { get; set; }
    public Guid? ToolId { get; set; }
    public Guid? AgentId { get; set; }
    public string InputMappingJson { get; set; } = "{}";
    public string ConfigurationJson { get; set; } = "{}";
    public bool ContinueOnError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
