using AgenticPlatform.Core.DTOs.Agents;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Agents;

public sealed class CreateAgentDtoValidator : AbstractValidator<CreateAgentDto>
{
    public CreateAgentDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.ProjectName)
            .MaximumLength(150);

        RuleFor(request => request.Role)
            .MaximumLength(150);

        RuleFor(request => request.Goal)
            .MaximumLength(2000);

        RuleFor(request => request.ExpectedOutput)
            .MaximumLength(2000);

        RuleFor(request => request.Tags)
            .MaximumLength(500);

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

        Include(new AgentAISettingsValidator<CreateAgentDto>());

        RuleFor(request => request.Status)
            .IsInEnum();
    }

}
