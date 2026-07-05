namespace AgenticPlatform.Core.DTOs.Realms;

public sealed class RealmDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAdminOnly { get; set; }
}
