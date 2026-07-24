namespace AgenticPlatform.Core.DTOs.Auth;

public sealed class ConfirmEmailDto
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
}
