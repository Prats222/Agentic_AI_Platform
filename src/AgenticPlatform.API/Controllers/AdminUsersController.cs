using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Admin;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using AgenticPlatform.Infrastructure.Data.Seed;
using AgenticPlatform.Infrastructure.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/v{version:apiVersion}/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITransactionalEmailService _emailService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ITransactionalEmailService emailService,
        ILogger<AdminUsersController> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<UserAccessDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserAccessDto>>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .ToListAsync(cancellationToken);
        var rolesByUserId = await GetRolesByUserIdAsync(
            users.Select(user => user.Id),
            cancellationToken);
        var result = users
            .Select(user => Map(user, GetRoles(user.Id, rolesByUserId)))
            .ToArray();

        return Ok(ApiResponse<IReadOnlyCollection<UserAccessDto>>.Ok(result));
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<AdminUsersPageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUsersPageDto>>> GetPagedUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] int timezoneOffsetMinutes = 0,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = pageSize is 25 or 50 or 100 ? pageSize : 25;
        timezoneOffsetMinutes = Math.Clamp(timezoneOffsetMinutes, -840, 840);

        var offset = TimeSpan.FromMinutes(timezoneOffsetMinutes);
        var localNow = DateTimeOffset.UtcNow.ToOffset(offset);
        var localDayStart = new DateTimeOffset(localNow.Date, offset).ToUniversalTime();
        var localDayEnd = localDayStart.AddDays(1);

        var query = _dbContext.Users.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var joinedTodayCount = await query.CountAsync(
            user => user.CreatedAt >= localDayStart && user.CreatedAt < localDayEnd,
            cancellationToken);

        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var joinedToday = await query
            .Where(user => user.CreatedAt >= localDayStart && user.CreatedAt < localDayEnd)
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Take(12)
            .ToListAsync(cancellationToken);

        var rolesByUserId = await GetRolesByUserIdAsync(
            users.Select(user => user.Id).Concat(joinedToday.Select(user => user.Id)),
            cancellationToken);

        var result = new AdminUsersPageDto
        {
            Items = users.Select(user => Map(user, GetRoles(user.Id, rolesByUserId))).ToArray(),
            JoinedToday = joinedToday.Select(user => Map(user, GetRoles(user.Id, rolesByUserId))).ToArray(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            JoinedTodayCount = joinedTodayCount
        };

        return Ok(ApiResponse<AdminUsersPageDto>.Ok(result));
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

    [HttpPost("{id:guid}/welcome-guide")]
    [ProducesResponseType(typeof(ApiResponse<UserAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserAccessDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserAccessDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserAccessDto>>> SendWelcomeGuide(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(ApiResponse<UserAccessDto>.Fail("User was not found."));
        }

        if (!user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest(ApiResponse<UserAccessDto>.Fail(
                "The welcome guide can only be sent to a confirmed email address."));
        }

        try
        {
            await _emailService.SendWelcomeGuideAsync(
                user.Email,
                user.DisplayName,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send welcome guide to user {UserId}.", user.Id);
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<UserAccessDto>.Fail(
                    "The email provider could not deliver the welcome guide. Check the Brevo configuration and sender verification."));
        }

        user.WelcomeGuideEmailSentAt = DateTimeOffset.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(ApiResponse<UserAccessDto>.Fail(
                "The guide was delivered, but its delivery status could not be saved.",
                updateResult.Errors.Select(error => error.Description).ToArray()));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(ApiResponse<UserAccessDto>.Ok(
            Map(user, roles),
            "Welcome guide sent successfully."));
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
            EmailConfirmed = user.EmailConfirmed,
            WelcomeGuideEmailSentAt = user.WelcomeGuideEmailSentAt,
            CreatedAt = user.CreatedAt,
            IsDemoUser = DemoUserSeeder.IsDemoEmail(user.Email)
        };
    }

    private async Task<IReadOnlyDictionary<Guid, string[]>> GetRolesByUserIdAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string[]>();
        }

        var assignments = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Cast<string>()
                    .ToArray());
    }

    private static IEnumerable<string> GetRoles(
        Guid userId,
        IReadOnlyDictionary<Guid, string[]> rolesByUserId)
    {
        return rolesByUserId.TryGetValue(userId, out var roles) ? roles : [];
    }
}
