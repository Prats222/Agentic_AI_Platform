using AgenticPlatform.Core.DTOs.Auth;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Auth;

public sealed class ResendConfirmationEmailDtoValidator : AbstractValidator<ResendConfirmationEmailDto>
{
    public ResendConfirmationEmailDtoValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
