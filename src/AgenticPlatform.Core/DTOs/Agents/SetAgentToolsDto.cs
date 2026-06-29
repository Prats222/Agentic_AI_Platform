namespace AgenticPlatform.Core.DTOs.Agents;

public sealed class SetAgentToolsDto
{
    public IReadOnlyCollection<Guid> ToolIds { get; set; } = Array.Empty<Guid>();
}
