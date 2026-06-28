using AgenticPlatform.Core.DTOs.Auth;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Auth;

public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
