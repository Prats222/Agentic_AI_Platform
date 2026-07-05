using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.AI;

public sealed class AISettingsDto
{
    public Guid Id { get; set; }
    public AIProvider Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public bool HasGeminiApiKey { get; set; }
    public bool HasOpenRouterApiKey { get; set; }
    public bool HasGroqApiKey { get; set; }
    public bool HasDeepSeekApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
