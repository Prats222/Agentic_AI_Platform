using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class Agent : BaseEntity
{
    public Guid RealmId { get; set; }
    public Realm? Realm { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProjectName { get; set; }
    public string? Role { get; set; }
    public string? Goal { get; set; }
    public string? ExpectedOutput { get; set; }
    public string? Tags { get; set; }
    public string ModelProvider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelConfigJson { get; set; } = "{}";
    public string InputSchemaJson { get; set; } = "{}";
    public bool UseGlobalAISettings { get; set; } = true;
    public AIProvider? AIProvider { get; set; }
    public string? AIModel { get; set; }
    public double? AITemperature { get; set; }
    public int? AIMaxTokens { get; set; }
    public double? AITopP { get; set; }
    public string? AISystemPrompt { get; set; }
    public string? AIApiKey { get; set; }
    public string? AIBaseUrl { get; set; }
    public AgentStatus Status { get; set; } = AgentStatus.Draft;

    public ICollection<Tool> Tools { get; set; } = new List<Tool>();
    public ICollection<ContextDocument> ContextDocuments { get; set; } = new List<ContextDocument>();
    public ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
    public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
    public ICollection<Execution> Executions { get; set; } = new List<Execution>();
}
