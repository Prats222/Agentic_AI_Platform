using AgenticPlatform.API.Realms;
using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Realms;
using AgenticPlatform.Infrastructure.Data;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
[Route("api/v{version:apiVersion}/realms")]
public sealed class RealmsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public RealmsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<RealmDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RealmDto>>>> GetRealms(CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(ApplicationRoles.Admin);
        var realms = await _dbContext.Realms
            .AsNoTracking()
            .Where(realm => !realm.IsAdminOnly || isAdmin)
            .OrderBy(realm => realm.IsAdminOnly)
            .Select(realm => new RealmDto
            {
                Id = realm.Id,
                Name = realm.Name,
                Description = realm.Description,
                IsAdminOnly = realm.IsAdminOnly
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<RealmDto>>.Ok(realms));
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<RealmDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<RealmDto>>> GetCurrentRealm(CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var realm = await _dbContext.Realms
            .AsNoTracking()
            .Where(item => item.Id == realmId)
            .Select(item => new RealmDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                IsAdminOnly = item.IsAdminOnly
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(ApiResponse<RealmDto>.Ok(realm ?? new RealmDto
        {
            Id = ApplicationRealms.UserRealmId,
            Name = ApplicationRealms.UserRealmName,
            Description = "Shared workspace visible to all users and admins."
        }));
    }
}
