using AgenticPlatform.Core.DTOs.Chat;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Interfaces;

public interface IChatService
{
    Task<IReadOnlyCollection<ChatConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ChatConversation?> GetOwnedConversationAsync(Guid conversationId, Guid userId, bool includeMessages, CancellationToken cancellationToken = default);
    Task<ChatConversation> GetOrCreateConversationAsync(Guid? conversationId, Guid userId, string prompt, AIProvider provider, string model, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ChatConversation conversation, string role, string content, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
}
