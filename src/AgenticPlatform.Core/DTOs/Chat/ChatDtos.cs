using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Chat;

public sealed class ChatConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public AIProvider Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public IReadOnlyCollection<ChatMessageDto> Messages { get; set; } = Array.Empty<ChatMessageDto>();
}

public sealed class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StreamChatMessageDto
{
    public Guid? ConversationId { get; set; }
    public AIProvider Provider { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public double TopP { get; set; } = 0.9;
    public string SystemPrompt { get; set; } = "You are PratsPilot, a precise AI assistant for building agentic workflows.";
    public string? BaseUrl { get; set; }
}
