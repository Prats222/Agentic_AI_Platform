using AgenticPlatform.Core.DTOs.Agents;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Agents;

public sealed class UpdateAgentDtoValidator : AbstractValidator<UpdateAgentDto>
{
    public UpdateAgentDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.ModelProvider)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.ModelName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.ModelConfigJson)
            .NotEmpty()
            .Must(JsonValidator.BeValidJson)
            .WithMessage("ModelConfigJson must be valid JSON.");

        RuleFor(request => request.Status)
            .IsInEnum();
    }

}
