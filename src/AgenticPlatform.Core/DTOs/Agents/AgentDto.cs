using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Agents;

public sealed class AgentDto
{
    public Guid Id { get; set; }
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
    public bool UseGlobalAISettings { get; set; }
    public AIProvider? AIProvider { get; set; }
    public string? AIModel { get; set; }
    public double? AITemperature { get; set; }
    public int? AIMaxTokens { get; set; }
    public double? AITopP { get; set; }
    public string? AISystemPrompt { get; set; }
    public bool HasAIApiKey { get; set; }
    public string? AIBaseUrl { get; set; }
    public AgentStatus Status { get; set; }
    public IReadOnlyCollection<Guid> ToolIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyCollection<string> ToolNames { get; set; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
