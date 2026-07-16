using AgenticPlatform.Core.DTOs.Agents;
using AgenticPlatform.Core.Enums;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Agents;

internal sealed class AgentAISettingsValidator<T> : AbstractValidator<T>
    where T : class
{
    public AgentAISettingsValidator()
    {
        RuleFor(request => GetAIProvider(request))
            .IsInEnum()
            .Must(provider => provider is null or AIProvider.Gemini or AIProvider.OpenRouter or AIProvider.Groq or AIProvider.Cerebras)
            .WithMessage("Only Gemini, Groq, Cerebras, and OpenRouter are currently supported.")
            .When(request => GetAIProvider(request).HasValue)
            .WithName("AIProvider");

        RuleFor(request => GetAIModel(request))
            .MaximumLength(150)
            .WithName("AIModel");

        RuleFor(request => GetAITemperature(request))
            .InclusiveBetween(0, 2)
            .When(request => GetAITemperature(request).HasValue)
            .WithName("AITemperature");

        RuleFor(request => GetAIMaxTokens(request))
            .InclusiveBetween(1, 200000)
            .When(request => GetAIMaxTokens(request).HasValue)
            .WithName("AIMaxTokens");

        RuleFor(request => GetAITopP(request))
            .InclusiveBetween(0, 1)
            .When(request => GetAITopP(request).HasValue)
            .WithName("AITopP");

        RuleFor(request => GetAISystemPrompt(request))
            .MaximumLength(8000)
            .WithName("AISystemPrompt");

        RuleFor(request => GetAIApiKey(request))
            .MaximumLength(4000)
            .WithName("AIApiKey");

        RuleFor(request => GetAIBaseUrl(request))
            .MaximumLength(500)
            .Must(value => string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out _))
            .WithMessage("AIBaseUrl must be a valid absolute URL.")
            .WithName("AIBaseUrl");

        When(request => !GetUseGlobalAISettings(request), () =>
        {
            RuleFor(request => GetAIProvider(request))
                .NotNull()
                .WithMessage("AIProvider is required when UseGlobalAISettings is false.")
                .WithName("AIProvider");

            RuleFor(request => GetAIModel(request))
                .NotEmpty()
                .WithMessage("AIModel is required when UseGlobalAISettings is false.")
                .WithName("AIModel");
        });
    }

    private static bool GetUseGlobalAISettings(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.UseGlobalAISettings,
            UpdateAgentDto update => update.UseGlobalAISettings,
            _ => true
        };
    }

    private static AIProvider? GetAIProvider(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AIProvider,
            UpdateAgentDto update => update.AIProvider,
            _ => null
        };
    }

    private static string? GetAIModel(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AIModel,
            UpdateAgentDto update => update.AIModel,
            _ => null
        };
    }

    private static double? GetAITemperature(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AITemperature,
            UpdateAgentDto update => update.AITemperature,
            _ => null
        };
    }

    private static int? GetAIMaxTokens(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AIMaxTokens,
            UpdateAgentDto update => update.AIMaxTokens,
            _ => null
        };
    }

    private static double? GetAITopP(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AITopP,
            UpdateAgentDto update => update.AITopP,
            _ => null
        };
    }

    private static string? GetAISystemPrompt(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AISystemPrompt,
            UpdateAgentDto update => update.AISystemPrompt,
            _ => null
        };
    }

    private static string? GetAIApiKey(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AIApiKey,
            UpdateAgentDto update => update.AIApiKey,
            _ => null
        };
    }

    private static string? GetAIBaseUrl(T request)
    {
        return request switch
        {
            CreateAgentDto create => create.AIBaseUrl,
            UpdateAgentDto update => update.AIBaseUrl,
            _ => null
        };
    }
}
