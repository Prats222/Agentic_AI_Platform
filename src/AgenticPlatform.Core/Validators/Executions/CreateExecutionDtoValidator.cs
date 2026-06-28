using AgenticPlatform.Core.DTOs.Executions;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Executions;

public sealed class CreateExecutionDtoValidator : AbstractValidator<CreateExecutionDto>
{
    public CreateExecutionDtoValidator()
    {
        RuleFor(request => request.TargetType)
            .IsInEnum();

        RuleFor(request => request.TargetId)
            .NotEmpty();

        RuleFor(request => request.InputJson)
            .NotEmpty()
            .Must(JsonValidator.BeValidJson)
            .WithMessage("InputJson must be valid JSON.");
    }
}
