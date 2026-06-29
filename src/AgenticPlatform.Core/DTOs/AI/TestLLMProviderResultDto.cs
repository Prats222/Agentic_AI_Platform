using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.AI;

public sealed class TestLLMProviderResultDto
{
    public AIProvider Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? RawResponseJson { get; set; }
}
