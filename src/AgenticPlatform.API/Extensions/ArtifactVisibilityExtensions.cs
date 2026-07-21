using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.API.Extensions;

public static class ArtifactVisibilityExtensions
{
    public static IQueryable<TArtifact> VisibleTo<TArtifact>(
        this IQueryable<TArtifact> query,
        Guid? userId,
        bool isAdmin)
        where TArtifact : ArtifactEntity
    {
        return isAdmin
            ? query
            : query.Where(artifact => artifact.Visibility == ArtifactVisibility.Realm
                || artifact.CreatedByUserId == null
                || artifact.CreatedByUserId == userId);
    }

    public static bool CanViewArtifact(this System.Security.Claims.ClaimsPrincipal user, ArtifactEntity artifact)
    {
        return user.IsAdmin()
            || artifact.Visibility == ArtifactVisibility.Realm
            || artifact.CreatedByUserId is null
            || artifact.CreatedByUserId == user.GetUserId();
    }
}
