namespace AgenticPlatform.Core.DTOs.Admin;

public sealed class UserAccessDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public bool CanAccessUserRealm { get; set; } = true;
    public bool CanAccessAdminRealm { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
