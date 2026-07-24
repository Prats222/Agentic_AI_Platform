using AgenticPlatform.Core.DTOs.Auth;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Auth;

public sealed class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
{
    public ConfirmEmailDtoValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.Code).NotEmpty().MaximumLength(4096);
    }
}
