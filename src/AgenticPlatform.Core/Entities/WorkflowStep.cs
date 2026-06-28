using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class WorkflowStep : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public Workflow Workflow { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public WorkflowStepType StepType { get; set; }

    public Guid? ToolId { get; set; }
    public Tool? Tool { get; set; }

    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }

    public string InputMappingJson { get; set; } = "{}";
    public string ConfigurationJson { get; set; } = "{}";
    public bool ContinueOnError { get; set; }
}
