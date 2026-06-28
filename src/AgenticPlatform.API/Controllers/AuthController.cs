using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Auth;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Settings;
using AgenticPlatform.Infrastructure.Data;
using AgenticPlatform.Infrastructure.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtOptions.Value;
    }

    [HttpPost("register")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        RegisterDto request,
        CancellationToken cancellationToken)
    {
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Requested role does not exist."));
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict(ApiResponse<AuthResponseDto>.Fail("A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(
                "User registration failed.",
                createResult.Errors.Select(error => error.Description).ToArray()));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(
                "User was created, but role assignment failed.",
                roleResult.Errors.Select(error => error.Description).ToArray()));
        }

        var authResponse = await CreateAuthResponseAsync(user, cancellationToken);

        return CreatedAtAction(
            nameof(Register),
            new { version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
            ApiResponse<AuthResponseDto>.Ok(authResponse, "User registered successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        LoginDto request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password."));
        }

        var authResponse = await CreateAuthResponseAsync(user, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful."));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        RefreshTokenDto request,
        CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Refresh token is invalid or expired."));
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user is null)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Refresh token user no longer exists."));
        }

        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.ReplacedByToken = newRefreshToken;

        var authResponse = await CreateAuthResponseAsync(user, cancellationToken, newRefreshToken);

        return Ok(ApiResponse<AuthResponseDto>.Ok(authResponse, "Token refreshed successfully."));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        RefreshTokenDto request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Authenticated user id was not found."));
        }

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(
                token => token.UserId == userId && token.Token == request.RefreshToken,
                cancellationToken);

        if (refreshToken is not null && refreshToken.IsActive)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Logout successful."));
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(
        ApplicationUser user,
        CancellationToken cancellationToken,
        string? refreshTokenValue = null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var refreshToken = refreshTokenValue ?? _jwtTokenService.GenerateRefreshToken();

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            roles,
            accessTokenExpiresAt);

        await _dbContext.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshTokenExpiresAt
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToArray()
        };
    }
}
