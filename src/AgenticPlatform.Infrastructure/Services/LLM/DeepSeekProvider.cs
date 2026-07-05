using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public sealed class DeepSeekProvider : OpenAICompatibleProviderBase
{
    public DeepSeekProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, "DeepSeek", "https://api.deepseek.com")
    {
    }

    public override AIProvider Provider => AIProvider.DeepSeek;
}
