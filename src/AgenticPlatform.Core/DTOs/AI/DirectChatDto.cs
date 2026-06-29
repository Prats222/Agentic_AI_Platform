using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.AI;

public sealed class DirectChatDto
{
    public AIProvider Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public double TopP { get; set; } = 0.9;
    public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
}
