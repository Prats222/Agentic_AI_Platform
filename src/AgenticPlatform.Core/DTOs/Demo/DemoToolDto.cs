namespace AgenticPlatform.Core.DTOs.Demo;

public sealed class DemoToolDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SampleInputJson { get; set; } = "{}";
}
