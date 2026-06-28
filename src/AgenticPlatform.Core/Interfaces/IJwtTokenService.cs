namespace AgenticPlatform.Core.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, DateTimeOffset expiresAt);
    string GenerateRefreshToken();
}
