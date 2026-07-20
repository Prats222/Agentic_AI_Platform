namespace AgenticPlatform.Core.Entities;

public sealed class Tool : ArtifactEntity
{
    public Guid RealmId { get; set; }
    public Realm? Realm { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string InputSchemaJson { get; set; } = "{}";
    public string EndpointUrl { get; set; } = string.Empty;
    public string SecretJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}
