using System.Security.Claims;
using AgenticPlatform.Core.Constants;

namespace AgenticPlatform.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        return user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Email) ?? "Unknown user";
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole(ApplicationRoles.Admin);
    }

    public static bool CanModifyArtifact(this ClaimsPrincipal user, Guid? createdByUserId)
    {
        return user.IsAdmin() || createdByUserId is null || createdByUserId == user.GetUserId();
    }
}
