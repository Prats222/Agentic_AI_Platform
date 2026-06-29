namespace AgenticPlatform.Core.Models.LLM;

public sealed class LLMChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string? RawResponseJson { get; set; }
}
