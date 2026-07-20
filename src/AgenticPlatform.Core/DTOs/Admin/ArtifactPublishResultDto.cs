using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Admin;

public sealed class ArtifactPublishResultDto
{
    public ArtifactType ArtifactType { get; set; }
    public Guid SourceArtifactId { get; set; }
    public Guid PublishedArtifactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool WasCreated { get; set; }
    public int PublishedDependencyCount { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}
