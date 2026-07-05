namespace AgenticPlatform.Core.DTOs.Agents;

public sealed class SetAgentContextDocumentsDto
{
    public IReadOnlyCollection<Guid> ContextDocumentIds { get; set; } = Array.Empty<Guid>();
}
