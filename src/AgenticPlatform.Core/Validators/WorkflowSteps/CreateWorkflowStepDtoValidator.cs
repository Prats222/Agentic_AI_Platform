using AgenticPlatform.Core.DTOs.WorkflowSteps;
using AgenticPlatform.Core.Enums;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.WorkflowSteps;

public sealed class CreateWorkflowStepDtoValidator : AbstractValidator<CreateWorkflowStepDto>
{
    public CreateWorkflowStepDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.Order)
            .GreaterThan(0);

        RuleFor(request => request.StepType)
            .IsInEnum();

        RuleFor(request => request.ToolId)
            .NotNull()
            .When(request => request.StepType == WorkflowStepType.Tool)
            .WithMessage("ToolId is required for Tool steps.");

        RuleFor(request => request.AgentId)
            .NotNull()
            .When(request => request.StepType == WorkflowStepType.Agent)
            .WithMessage("AgentId is required for Agent steps.");

        RuleFor(request => request)
            .Must(request => request.StepType != WorkflowStepType.Tool || request.AgentId is null)
            .WithMessage("AgentId must be empty for Tool steps.");

        RuleFor(request => request)
            .Must(request => request.StepType != WorkflowStepType.Agent || request.ToolId is null)
            .WithMessage("ToolId must be empty for Agent steps.");

        RuleFor(request => request.InputMappingJson)
            .NotEmpty()
            .Must(JsonValidator.BeValidJson)
            .WithMessage("InputMappingJson must be valid JSON.");

        RuleFor(request => request.ConfigurationJson)
            .NotEmpty()
            .Must(JsonValidator.BeValidJson)
            .WithMessage("ConfigurationJson must be valid JSON.");
    }
}
