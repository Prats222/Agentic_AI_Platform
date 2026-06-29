using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.AI;

public sealed class ResolvedAISettingsDto
{
    public AIProvider Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public bool UsesGlobalSettings { get; set; }
}
