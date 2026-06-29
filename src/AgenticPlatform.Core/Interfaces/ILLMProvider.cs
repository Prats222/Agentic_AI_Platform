using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Models.LLM;

namespace AgenticPlatform.Core.Interfaces;

public interface ILLMProvider
{
    AIProvider Provider { get; }
    Task<LLMChatResponse> ChatAsync(LLMChatRequest request, CancellationToken cancellationToken = default);
}
