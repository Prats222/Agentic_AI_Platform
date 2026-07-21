using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public abstract class ArtifactEntity : BaseEntity
{
    public ArtifactVisibility Visibility { get; set; } = ArtifactVisibility.Realm;
    public Guid? PublishedFromArtifactId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public string? PublishedByDisplayName { get; set; }
}
