using AgenticPlatform.Core.DTOs.Chat;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AgenticPlatform.Core.Settings;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class ChatService : IChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly int _maximumConversationsPerUser;
    private readonly int _maximumMessagesPerConversation;
    private readonly int _maximumMessageLength;

    public ChatService(ApplicationDbContext dbContext, IOptions<ChatHistorySettings> options)
    {
        _dbContext = dbContext;
        _maximumConversationsPerUser = Math.Clamp(options.Value.MaximumConversationsPerUser, 1, 10);
        _maximumMessagesPerConversation = Math.Clamp(options.Value.MaximumMessagesPerConversation, 2, 100);
        _maximumMessageLength = Math.Clamp(options.Value.MaximumMessageLength, 500, 8000);
    }

    public async Task<IReadOnlyCollection<ChatConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatConversations
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new ChatConversationDto
            {
                Id = item.Id,
                Title = item.Title,
                Provider = item.Provider,
                Model = item.Model,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Messages = item.Messages
                    .OrderBy(message => message.CreatedAt)
                    .Select(message => new ChatMessageDto
                    {
                        Id = message.Id,
                        Role = message.Role,
                        Content = message.Content,
                        CreatedAt = message.CreatedAt
                    })
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);
    }

    public Task<ChatConversation?> GetOwnedConversationAsync(
        Guid conversationId,
        Guid userId,
        bool includeMessages,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ChatConversation> query = _dbContext.ChatConversations;
        if (includeMessages)
        {
            query = query.Include(item => item.Messages.OrderBy(message => message.CreatedAt));
        }

        return query.FirstOrDefaultAsync(item => item.Id == conversationId && item.UserId == userId, cancellationToken);
    }

    public async Task<ChatConversation> GetOrCreateConversationAsync(
        Guid? conversationId,
        Guid userId,
        string prompt,
        AIProvider provider,
        string model,
        CancellationToken cancellationToken = default)
    {
        if (conversationId.HasValue)
        {
            var existing = await GetOwnedConversationAsync(conversationId.Value, userId, true, cancellationToken);
            return existing ?? throw new KeyNotFoundException("Chat conversation was not found.");
        }

        var conversations = await _dbContext.ChatConversations
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .ToArrayAsync(cancellationToken);

        foreach (var staleConversation in conversations.Skip(_maximumConversationsPerUser - 1))
        {
            _dbContext.ChatConversations.Remove(staleConversation);
        }

        var conversation = new ChatConversation
        {
            UserId = userId,
            Title = BuildTitle(prompt),
            Provider = provider,
            Model = model.Trim()
        };
        await _dbContext.ChatConversations.AddAsync(conversation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task AddMessageAsync(ChatConversation conversation, string role, string content, CancellationToken cancellationToken = default)
    {
        var normalizedContent = content.Trim();
        if (normalizedContent.Length > _maximumMessageLength)
        {
            normalizedContent = normalizedContent[.._maximumMessageLength];
        }

        var existingMessages = await _dbContext.ChatMessages
            .Where(item => item.ConversationId == conversation.Id)
            .OrderBy(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken);

        var overflow = Math.Max(0, existingMessages.Length - _maximumMessagesPerConversation + 1);
        if (overflow > 0)
        {
            _dbContext.ChatMessages.RemoveRange(existingMessages.Take(overflow));
        }

        await _dbContext.ChatMessages.AddAsync(new ChatMessage
        {
            ConversationId = conversation.Id,
            Conversation = conversation,
            Role = role,
            Content = normalizedContent
        }, cancellationToken);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await GetOwnedConversationAsync(conversationId, userId, false, cancellationToken);
        if (conversation is null)
        {
            return;
        }

        _dbContext.ChatConversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildTitle(string prompt)
    {
        var title = string.Join(' ', prompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return title.Length <= 60 ? title : $"{title[..57]}...";
    }
}
