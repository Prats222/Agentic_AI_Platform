using AgenticPlatform.Core.DTOs.Admin;
using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Interfaces;

public interface IArtifactPublishingService
{
    Task<ArtifactPublishResultDto?> PublishAsync(
        ArtifactType artifactType,
        Guid sourceArtifactId,
        Guid publishedByUserId,
        string publishedByDisplayName,
        CancellationToken cancellationToken = default);
}
