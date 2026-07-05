namespace AgenticPlatform.Core.Entities;

public sealed class Realm : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAdminOnly { get; set; }

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
    public ICollection<Tool> Tools { get; set; } = new List<Tool>();
    public ICollection<ContextDocument> ContextDocuments { get; set; } = new List<ContextDocument>();
    public ICollection<Execution> Executions { get; set; } = new List<Execution>();
    public ICollection<ArenaChallenge> ArenaChallenges { get; set; } = new List<ArenaChallenge>();
}
