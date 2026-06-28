using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Agents;

public sealed class AgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ModelProvider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelConfigJson { get; set; } = "{}";
    public AgentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
