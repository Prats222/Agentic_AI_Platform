using AgenticPlatform.Core.DTOs.AI;
using AgenticPlatform.Core.Models.LLM;

namespace AgenticPlatform.Core.Interfaces;

public interface IAISettingsService
{
    Task<AISettingsDto> GetGlobalSettingsAsync(CancellationToken cancellationToken = default);
    Task<AISettingsDto> UpdateGlobalSettingsAsync(UpdateAISettingsDto request, CancellationToken cancellationToken = default);
    Task<ResolvedAISettingsDto?> GetResolvedSettingsAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<LLMChatRequest?> BuildChatRequestAsync(Guid? agentId, string prompt, CancellationToken cancellationToken = default);
    Task<LLMChatRequest> BuildDirectChatRequestAsync(DirectChatDto request, CancellationToken cancellationToken = default);
}
