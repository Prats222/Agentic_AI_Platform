using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Admin;
using AgenticPlatform.Infrastructure.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/v{version:apiVersion}/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<UserAccessDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserAccessDto>>>> GetUsers()
    {
        var users = _userManager.Users.OrderBy(user => user.Email).ToList();
        var result = new List<UserAccessDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(Map(user, roles));
        }

        return Ok(ApiResponse<IReadOnlyCollection<UserAccessDto>>.Ok(result));
    }

    [HttpPut("{id:guid}/access")]
    [ProducesResponseType(typeof(ApiResponse<UserAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserAccessDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserAccessDto>>> UpdateAccess(Guid id, UpdateUserAccessDto request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(ApiResponse<UserAccessDto>.Fail("User was not found."));
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin);
        if (request.IsAdmin && !isAdmin)
        {
            var result = await _userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<UserAccessDto>.Fail("Could not grant admin access.", result.Errors.Select(error => error.Description).ToArray()));
            }
        }

        if (!request.IsAdmin && isAdmin)
        {
            var result = await _userManager.RemoveFromRoleAsync(user, ApplicationRoles.Admin);
            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse<UserAccessDto>.Fail("Could not remove admin access.", result.Errors.Select(error => error.Description).ToArray()));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(ApiResponse<UserAccessDto>.Ok(Map(user, roles), "User access updated successfully."));
    }

    private static UserAccessDto Map(ApplicationUser user, IEnumerable<string> roles)
    {
        var roleArray = roles.ToArray();
        return new UserAccessDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roleArray,
            CanAccessUserRealm = true,
            CanAccessAdminRealm = roleArray.Contains(ApplicationRoles.Admin),
            CreatedAt = user.CreatedAt
        };
    }
}
