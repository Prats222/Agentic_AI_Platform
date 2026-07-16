using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class AISettings : BaseEntity
{
    public AIProvider Provider { get; set; } = AIProvider.Gemini;
    public string Model { get; set; } = "gemini-3.1-flash-lite";
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public double TopP { get; set; } = 0.9;
    public string SystemPrompt { get; set; } = "You are a helpful AI agent.";
    public string? ApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
    public string? OpenRouterApiKey { get; set; }
    public string? GroqApiKey { get; set; }
    public string? CerebrasApiKey { get; set; }
    public string? DeepSeekApiKey { get; set; }
    public string? BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}
