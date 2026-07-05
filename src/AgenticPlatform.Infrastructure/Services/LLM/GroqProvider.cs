using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public sealed class GroqProvider : OpenAICompatibleProviderBase
{
    public GroqProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, "Groq", "https://api.groq.com/openai/v1")
    {
    }

    public override AIProvider Provider => AIProvider.Groq;
}
