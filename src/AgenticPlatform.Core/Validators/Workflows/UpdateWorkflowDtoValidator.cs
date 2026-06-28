using AgenticPlatform.Core.DTOs.Workflows;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Workflows;

public sealed class UpdateWorkflowDtoValidator : AbstractValidator<UpdateWorkflowDto>
{
    public UpdateWorkflowDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.Status)
            .IsInEnum();
    }
}
