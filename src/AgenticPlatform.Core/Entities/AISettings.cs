using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class AISettings : BaseEntity
{
    public AIProvider Provider { get; set; } = AIProvider.Ollama;
    public string Model { get; set; } = "llama3.1";
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public double TopP { get; set; } = 0.9;
    public string SystemPrompt { get; set; } = "You are a helpful AI agent.";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; } = "http://localhost:11434";
}
