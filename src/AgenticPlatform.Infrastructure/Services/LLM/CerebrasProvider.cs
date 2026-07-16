using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public sealed class CerebrasProvider : OpenAICompatibleProviderBase
{
    public CerebrasProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, "Cerebras", "https://api.cerebras.ai/v1")
    {
    }

    public override AIProvider Provider => AIProvider.Cerebras;
    protected override string MaxTokensParameter => "max_completion_tokens";
}
