using AgenticPlatform.Core.DTOs.AI;
using AgenticPlatform.Core.Enums;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.AI;

public sealed class DirectChatDtoValidator : AbstractValidator<DirectChatDto>
{
    public DirectChatDtoValidator()
    {
        RuleFor(request => request.Provider)
            .IsInEnum()
            .Must(provider => provider is AIProvider.Gemini or AIProvider.OpenRouter or AIProvider.Groq)
            .WithMessage("Only Gemini, Groq, and OpenRouter are currently supported.");

        RuleFor(request => request.Model)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Prompt)
            .NotEmpty()
            .MaximumLength(16000);

        RuleFor(request => request.Temperature)
            .InclusiveBetween(0, 2);

        RuleFor(request => request.MaxTokens)
            .InclusiveBetween(1, 200000);

        RuleFor(request => request.TopP)
            .InclusiveBetween(0, 1);

        RuleFor(request => request.SystemPrompt)
            .NotEmpty()
            .MaximumLength(8000);

        RuleFor(request => request.ApiKey)
            .MaximumLength(4000);

        RuleFor(request => request.BaseUrl)
            .MaximumLength(500)
            .Must(value => string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out _))
            .WithMessage("BaseUrl must be a valid absolute URL.");
    }
}
