using AgenticPlatform.Core.DTOs.AI;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.LLM;
using AgenticPlatform.Infrastructure.Data;
using AgenticPlatform.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class AISettingsService : IAISettingsService
{
    private readonly ApplicationDbContext _dbContext;

    public AISettingsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AISettingsDto> GetGlobalSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<AISettingsDto> UpdateGlobalSettingsAsync(UpdateAISettingsDto request, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateGlobalSettingsAsync(cancellationToken);

        settings.Provider = request.Provider;
        settings.Model = request.Model.Trim();
        settings.Temperature = request.Temperature;
        settings.MaxTokens = request.MaxTokens;
        settings.TopP = request.TopP;
        settings.SystemPrompt = request.SystemPrompt.Trim();
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            SetProviderApiKey(settings, request.Provider, request.ApiKey.Trim());
            settings.ApiKey = request.ApiKey.Trim();
        }
        settings.BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? null : request.BaseUrl.Trim();
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<ResolvedAISettingsDto?> GetResolvedSettingsAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == agentId, cancellationToken);

        if (agent is null)
        {
            return null;
        }

        var globalSettings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        return Resolve(agent, globalSettings);
    }

    public async Task<LLMChatRequest?> BuildChatRequestAsync(Guid? agentId, string prompt, CancellationToken cancellationToken = default)
    {
        var globalSettings = await GetOrCreateGlobalSettingsAsync(cancellationToken);
        Agent? agent = null;

        if (agentId.HasValue)
        {
            agent = await _dbContext.Agents
                .AsNoTracking()
                .Include(item => item.Tools)
                .Include(item => item.ContextDocuments)
                .FirstOrDefaultAsync(item => item.Id == agentId.Value, cancellationToken);

            if (agent is null)
            {
                return null;
            }
        }

        var resolved = agent is null
            ? Resolve(globalSettings)
            : Resolve(agent, globalSettings);

        return new LLMChatRequest
        {
            Provider = resolved.Provider,
            Model = resolved.Model,
            Temperature = resolved.Temperature,
            MaxTokens = resolved.MaxTokens,
            TopP = resolved.TopP,
            SystemPrompt = BuildSystemPrompt(resolved.SystemPrompt, agent),
            ApiKey = agent?.UseGlobalAISettings == false
                ? string.IsNullOrWhiteSpace(agent.AIApiKey) ? GetProviderApiKey(globalSettings, resolved.Provider) : agent.AIApiKey
                : GetProviderApiKey(globalSettings, resolved.Provider),
            BaseUrl = resolved.BaseUrl,
            Messages =
            [
                new LLMChatMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ]
        };
    }

    public async Task<LLMChatRequest> BuildDirectChatRequestAsync(DirectChatDto request, CancellationToken cancellationToken = default)
    {
        var globalSettings = await GetOrCreateGlobalSettingsAsync(cancellationToken);

        return new LLMChatRequest
        {
            Provider = request.Provider,
            Model = request.Model.Trim(),
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            TopP = request.TopP,
            SystemPrompt = request.SystemPrompt.Trim(),
            ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? GetProviderApiKey(globalSettings, request.Provider) : request.ApiKey.Trim(),
            BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? globalSettings.BaseUrl : request.BaseUrl.Trim(),
            Messages =
            [
                new LLMChatMessage
                {
                    Role = "user",
                    Content = request.Prompt
                }
            ]
        };
    }

    private async Task<AISettings> GetOrCreateGlobalSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.AISettings
            .FirstOrDefaultAsync(item => item.Id == AISettingsConfiguration.GlobalSettingsId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new AISettings
        {
            Id = AISettingsConfiguration.GlobalSettingsId
        };

        await _dbContext.AISettings.AddAsync(settings, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static AISettingsDto Map(AISettings settings)
    {
        return new AISettingsDto
        {
            Id = settings.Id,
            Provider = settings.Provider,
            Model = settings.Model,
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            TopP = settings.TopP,
            SystemPrompt = settings.SystemPrompt,
            HasApiKey = !string.IsNullOrWhiteSpace(GetProviderApiKey(settings, settings.Provider)),
            HasGeminiApiKey = !string.IsNullOrWhiteSpace(settings.GeminiApiKey) || (settings.Provider == Core.Enums.AIProvider.Gemini && !string.IsNullOrWhiteSpace(settings.ApiKey)),
            HasOpenRouterApiKey = !string.IsNullOrWhiteSpace(settings.OpenRouterApiKey) || (settings.Provider == Core.Enums.AIProvider.OpenRouter && !string.IsNullOrWhiteSpace(settings.ApiKey)),
            HasGroqApiKey = !string.IsNullOrWhiteSpace(settings.GroqApiKey) || (settings.Provider == Core.Enums.AIProvider.Groq && !string.IsNullOrWhiteSpace(settings.ApiKey)),
            HasDeepSeekApiKey = !string.IsNullOrWhiteSpace(settings.DeepSeekApiKey) || (settings.Provider == Core.Enums.AIProvider.DeepSeek && !string.IsNullOrWhiteSpace(settings.ApiKey)),
            BaseUrl = settings.BaseUrl,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private static ResolvedAISettingsDto Resolve(AISettings settings)
    {
        return new ResolvedAISettingsDto
        {
            Provider = settings.Provider,
            Model = settings.Model,
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            TopP = settings.TopP,
            SystemPrompt = settings.SystemPrompt,
            HasApiKey = !string.IsNullOrWhiteSpace(GetProviderApiKey(settings, settings.Provider)),
            BaseUrl = settings.BaseUrl,
            UsesGlobalSettings = true
        };
    }

    private static ResolvedAISettingsDto Resolve(Agent agent, AISettings globalSettings)
    {
        if (agent.UseGlobalAISettings)
        {
            return Resolve(globalSettings);
        }

        return new ResolvedAISettingsDto
        {
            Provider = agent.AIProvider ?? globalSettings.Provider,
            Model = string.IsNullOrWhiteSpace(agent.AIModel) ? globalSettings.Model : agent.AIModel,
            Temperature = agent.AITemperature ?? globalSettings.Temperature,
            MaxTokens = agent.AIMaxTokens ?? globalSettings.MaxTokens,
            TopP = agent.AITopP ?? globalSettings.TopP,
            SystemPrompt = string.IsNullOrWhiteSpace(agent.AISystemPrompt)
                ? globalSettings.SystemPrompt
                : agent.AISystemPrompt,
            HasApiKey = !string.IsNullOrWhiteSpace(agent.AIApiKey) || !string.IsNullOrWhiteSpace(GetProviderApiKey(globalSettings, agent.AIProvider ?? globalSettings.Provider)),
            BaseUrl = string.IsNullOrWhiteSpace(agent.AIBaseUrl) ? globalSettings.BaseUrl : agent.AIBaseUrl,
            UsesGlobalSettings = false
        };
    }

    private static string BuildSystemPrompt(string systemPrompt, Agent? agent)
    {
        if (agent is null)
        {
            return systemPrompt;
        }

        var sections = new List<string> { systemPrompt };
        sections.Add($"""
Agent profile:
- Name: {agent.Name}
- Project: {Fallback(agent.ProjectName)}
- Role: {Fallback(agent.Role)}
- Goal: {Fallback(agent.Goal)}
- Description: {Fallback(agent.Description)}
- Expected output: {Fallback(agent.ExpectedOutput)}
- Tags: {Fallback(agent.Tags)}

Follow the agent profile strictly. If the expected output asks for code only, return only code with no explanation, no markdown fence, and no prose. Prefer direct completion of the user's request over describing how it could be done.
""");

        if (agent.Tools.Count > 0)
        {
            var tools = string.Join(
                Environment.NewLine,
                agent.Tools.Select(tool => $"- {tool.Name} ({tool.Category}): {tool.Description ?? "No description"}"));
            sections.Add($"""
Available tools assigned to this agent:
{tools}

Use assigned tools only when they are genuinely needed. Do not claim a tool was called unless the runtime has actually provided a tool result.
""");
        }

        if (agent.ContextDocuments.Count > 0)
        {
            var context = string.Join(
                Environment.NewLine + Environment.NewLine,
                agent.ContextDocuments.Select(document => $"""
Document: {document.Name}
{Truncate(document.ExtractedText, 6000)}
"""));
            sections.Add($"""
Attached context documents:
{context}

Treat attached context documents as authoritative project knowledge. If the user asks for standards, templates, structure, or examples that appear in context, follow the context instead of generic defaults.
""");
        }

        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not specified" : value.Trim();
    }

    private static string? GetProviderApiKey(AISettings settings, Core.Enums.AIProvider provider)
    {
        return provider switch
        {
            Core.Enums.AIProvider.Gemini => FirstNonEmpty(settings.GeminiApiKey, settings.Provider == provider ? settings.ApiKey : null),
            Core.Enums.AIProvider.OpenRouter => FirstNonEmpty(settings.OpenRouterApiKey, settings.Provider == provider ? settings.ApiKey : null),
            Core.Enums.AIProvider.Groq => FirstNonEmpty(settings.GroqApiKey, settings.Provider == provider ? settings.ApiKey : null),
            Core.Enums.AIProvider.DeepSeek => FirstNonEmpty(settings.DeepSeekApiKey, settings.Provider == provider ? settings.ApiKey : null),
            _ => settings.ApiKey
        };
    }

    private static void SetProviderApiKey(AISettings settings, Core.Enums.AIProvider provider, string apiKey)
    {
        switch (provider)
        {
            case Core.Enums.AIProvider.Gemini:
                settings.GeminiApiKey = apiKey;
                break;
            case Core.Enums.AIProvider.OpenRouter:
                settings.OpenRouterApiKey = apiKey;
                break;
            case Core.Enums.AIProvider.Groq:
                settings.GroqApiKey = apiKey;
                break;
            case Core.Enums.AIProvider.DeepSeek:
                settings.DeepSeekApiKey = apiKey;
                break;
            default:
                settings.ApiKey = apiKey;
                break;
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
