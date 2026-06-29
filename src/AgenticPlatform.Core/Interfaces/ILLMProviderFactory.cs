using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Interfaces;

public interface ILLMProviderFactory
{
    ILLMProvider GetProvider(AIProvider provider);
}
