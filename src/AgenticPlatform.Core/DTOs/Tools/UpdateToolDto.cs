using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Tools;

public sealed class UpdateToolDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string InputSchemaJson { get; set; } = "{}";
    public string EndpointUrl { get; set; } = string.Empty;
    public string? SecretJson { get; set; }
    public bool IsEnabled { get; set; }
    public ArtifactVisibility Visibility { get; set; } = ArtifactVisibility.Realm;
}
