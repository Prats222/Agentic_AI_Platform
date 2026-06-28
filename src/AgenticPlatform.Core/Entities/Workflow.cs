using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class Workflow : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public ICollection<Execution> Executions { get; set; } = new List<Execution>();
}
