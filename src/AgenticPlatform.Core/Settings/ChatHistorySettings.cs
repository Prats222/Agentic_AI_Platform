namespace AgenticPlatform.Core.Settings;

public sealed class ChatHistorySettings
{
    public int MaximumConversationsPerUser { get; set; } = 2;
    public int MaximumMessagesPerConversation { get; set; } = 20;
    public int MaximumMessageLength { get; set; } = 8000;
}
