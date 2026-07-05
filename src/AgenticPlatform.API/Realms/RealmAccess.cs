using System.Security.Claims;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AgenticPlatform.API.Realms;

public static class RealmAccess
{
    public const string HeaderName = "X-Realm-Id";

    public static Guid ResolveRealmId(ControllerBase controller)
    {
        var rawRealmId = controller.Request.Headers[HeaderName].FirstOrDefault()
            ?? controller.Request.Query["realmId"].FirstOrDefault();

        return Guid.TryParse(rawRealmId, out var realmId)
            ? realmId
            : ApplicationRealms.UserRealmId;
    }

    public static bool CanAccessRealm(ClaimsPrincipal user, Guid realmId)
    {
        if (realmId == ApplicationRealms.UserRealmId)
        {
            return true;
        }

        if (realmId == ApplicationRealms.AdminRealmId)
        {
            return user.IsInRole(ApplicationRoles.Admin);
        }

        return false;
    }

    public static bool CanAccessRealm(ControllerBase controller, Guid realmId)
    {
        return CanAccessRealm(controller.User, realmId);
    }

    public static IQueryable<Agent> InRealm(this IQueryable<Agent> query, Guid realmId) =>
        query.Where(item => item.RealmId == realmId);

    public static IQueryable<Workflow> InRealm(this IQueryable<Workflow> query, Guid realmId) =>
        query.Where(item => item.RealmId == realmId);

    public static IQueryable<Tool> InRealm(this IQueryable<Tool> query, Guid realmId) =>
        query.Where(item => item.RealmId == realmId);

    public static IQueryable<ContextDocument> InRealm(this IQueryable<ContextDocument> query, Guid realmId) =>
        query.Where(item => item.RealmId == realmId);

    public static IQueryable<Execution> InRealm(this IQueryable<Execution> query, Guid realmId) =>
        query.Where(item => item.RealmId == realmId);
}
