using System.Text;
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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly ITransactionalEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ITransactionalEmailService emailService,
        IOptions<JwtSettings> jwtOptions,
        IOptions<EmailSettings> emailOptions,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _jwtSettings = jwtOptions.Value;
        _emailSettings = emailOptions.Value;
        _logger = logger;
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

        if (!user.EmailConfirmed)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(
                "Please confirm your email before signing in. You can resend the confirmation email from the login page."));
        }

        var authResponse = await CreateAuthResponseAsync(user, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful."));
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    [EnableRateLimiting("Email")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResultDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RegistrationResultDto>>> SignUp(
        SelfRegisterDto request,
        CancellationToken cancellationToken)
    {
        if (!await _roleManager.RoleExistsAsync(ApplicationRoles.Developer))
        {
            return BadRequest(ApiResponse<RegistrationResultDto>.Fail("Default user role does not exist."));
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict(ApiResponse<RegistrationResultDto>.Fail("A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(ApiResponse<RegistrationResultDto>.Fail(
                "User registration failed.",
                createResult.Errors.Select(error => error.Description).ToArray()));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.Developer);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(ApiResponse<RegistrationResultDto>.Fail(
                "User registration failed during role assignment.",
                roleResult.Errors.Select(error => error.Description).ToArray()));
        }

        var emailSent = await TrySendConfirmationEmailAsync(user, cancellationToken);
        var registrationResult = new RegistrationResultDto
        {
            Email = user.Email ?? request.Email,
            ConfirmationEmailSent = emailSent
        };
        var message = emailSent
            ? "Account created. Check your inbox to confirm your email."
            : "Account created, but the confirmation email could not be delivered. Use Resend confirmation after email delivery is configured.";

        return CreatedAtAction(
            nameof(SignUp),
            new { version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
            ApiResponse<RegistrationResultDto>.Ok(registrationResult, message));
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("Email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmEmail(
        ConfirmEmailDto request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return BadRequest(ApiResponse<object>.Fail("The confirmation link is invalid or expired."));
        }

        if (user.EmailConfirmed)
        {
            return Ok(ApiResponse<object>.Ok(new { }, "Your email is already confirmed. You can sign in."));
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            return BadRequest(ApiResponse<object>.Fail("The confirmation link is invalid or expired."));
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "The confirmation link is invalid or expired.",
                result.Errors.Select(error => error.Description).ToArray()));
        }

        await TrySendWelcomeGuideAsync(user, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Email confirmed. Welcome to PratsPilot."));
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting("Email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ResendConfirmation(
        ResendConfirmationEmailDto request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            await TrySendConfirmationEmailAsync(user, cancellationToken);
        }

        return Ok(ApiResponse<object>.Ok(
            new { },
            "If an unconfirmed account exists for that address, a new confirmation email has been sent."));
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

    private async Task<bool> TrySendConfirmationEmailAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var frontendUrl = _emailSettings.FrontendBaseUrl.TrimEnd('/');
            var confirmationUrl =
                $"{frontendUrl}/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(encodedToken)}";

            await _emailService.SendConfirmationEmailAsync(
                user.Email ?? string.Empty,
                user.DisplayName,
                confirmationUrl,
                cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send confirmation email to user {UserId}.", user.Id);
            return false;
        }
    }

    private async Task TrySendWelcomeGuideAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendWelcomeGuideAsync(
                user.Email ?? string.Empty,
                user.DisplayName,
                cancellationToken);
            user.WelcomeGuideEmailSentAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning(
                    "Welcome email was sent to user {UserId}, but its delivery timestamp could not be saved.",
                    user.Id);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Email was confirmed, but the welcome guide could not be sent to user {UserId}.", user.Id);
        }
    }
}
