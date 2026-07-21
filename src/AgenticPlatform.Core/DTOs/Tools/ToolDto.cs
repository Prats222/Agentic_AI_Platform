using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Tools;

public sealed class ToolDto
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string InputSchemaJson { get; set; } = "{}";
    public string EndpointUrl { get; set; } = string.Empty;
    public bool HasSecrets { get; set; }
    public bool IsEnabled { get; set; }
    public ArtifactVisibility Visibility { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByDisplayName { get; set; }
    public Guid? PublishedFromArtifactId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public string? PublishedByDisplayName { get; set; }
}
