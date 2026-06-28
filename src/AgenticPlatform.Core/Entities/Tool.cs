namespace AgenticPlatform.Core.Entities;

public sealed class Tool : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string InputSchemaJson { get; set; } = "{}";
    public string EndpointUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}
