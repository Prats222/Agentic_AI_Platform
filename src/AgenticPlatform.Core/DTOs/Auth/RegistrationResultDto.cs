namespace AgenticPlatform.Core.DTOs.Auth;

public sealed class RegistrationResultDto
{
    public string Email { get; set; } = string.Empty;
    public bool RequiresEmailConfirmation { get; set; } = true;
    public bool ConfirmationEmailSent { get; set; }
}
