using AgenticPlatform.Core.DTOs.Tools;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Tools;

public sealed class ExecuteToolDtoValidator : AbstractValidator<ExecuteToolDto>
{
    public ExecuteToolDtoValidator()
    {
        RuleFor(request => request.InputJson)
            .NotEmpty()
            .Must(JsonValidator.BeValidJson)
            .WithMessage("InputJson must be valid JSON.");
    }
}
