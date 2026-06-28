using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class Agent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ModelProvider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelConfigJson { get; set; } = "{}";
    public AgentStatus Status { get; set; } = AgentStatus.Draft;

    public ICollection<Tool> Tools { get; set; } = new List<Tool>();
    public ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
    public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
    public ICollection<Execution> Executions { get; set; } = new List<Execution>();
}
