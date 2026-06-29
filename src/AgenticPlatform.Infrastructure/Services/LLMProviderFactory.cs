using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IReadOnlyDictionary<AIProvider, ILLMProvider> _providers;

    public LLMProviderFactory(IEnumerable<ILLMProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.Provider);
    }

    public ILLMProvider GetProvider(AIProvider provider)
    {
        if (_providers.TryGetValue(provider, out var llmProvider))
        {
            return llmProvider;
        }

        throw new NotSupportedException($"LLM provider '{provider}' is not registered.");
    }
}
