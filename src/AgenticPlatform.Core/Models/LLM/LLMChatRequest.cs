using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Models.LLM;

public sealed class LLMChatRequest
{
    public AIProvider Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public IReadOnlyCollection<LLMChatMessage> Messages { get; set; } = Array.Empty<LLMChatMessage>();
}
