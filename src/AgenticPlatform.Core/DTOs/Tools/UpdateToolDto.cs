namespace AgenticPlatform.Core.DTOs.Tools;

public sealed class UpdateToolDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string InputSchemaJson { get; set; } = "{}";
    public string EndpointUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
