using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.LLM;

namespace AgenticPlatform.Infrastructure.Services.LLM;

public sealed class OpenRouterProvider : OpenAICompatibleProviderBase
{
    public OpenRouterProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, "OpenRouter", "https://openrouter.ai/api/v1")
    {
    }

    public override AIProvider Provider => AIProvider.OpenRouter;
}
