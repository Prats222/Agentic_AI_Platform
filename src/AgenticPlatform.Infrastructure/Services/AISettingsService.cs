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
        settings.ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? settings.ApiKey : request.ApiKey.Trim();
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
                ? string.IsNullOrWhiteSpace(agent.AIApiKey) ? globalSettings.ApiKey : agent.AIApiKey
                : globalSettings.ApiKey,
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
            ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? globalSettings.ApiKey : request.ApiKey.Trim(),
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
            HasApiKey = !string.IsNullOrWhiteSpace(settings.ApiKey),
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
            HasApiKey = !string.IsNullOrWhiteSpace(settings.ApiKey),
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
            HasApiKey = !string.IsNullOrWhiteSpace(agent.AIApiKey) || !string.IsNullOrWhiteSpace(globalSettings.ApiKey),
            BaseUrl = string.IsNullOrWhiteSpace(agent.AIBaseUrl) ? globalSettings.BaseUrl : agent.AIBaseUrl,
            UsesGlobalSettings = false
        };
    }

    private static string BuildSystemPrompt(string systemPrompt, Agent? agent)
    {
        if (agent?.Tools.Count > 0 != true)
        {
            return systemPrompt;
        }

        var tools = string.Join(
            Environment.NewLine,
            agent.Tools.Select(tool => $"- {tool.Name} ({tool.Category}): {tool.Description ?? "No description"}"));

        return $"""
{systemPrompt}

Available tools assigned to this agent:
{tools}

When useful, explain which assigned tool should be used and what input JSON it expects.
""";
    }
}
