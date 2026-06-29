namespace AgenticPlatform.Core.Models.LLM;

public sealed class LLMChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}
