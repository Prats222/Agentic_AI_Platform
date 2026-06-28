using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Auth;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Auth;

public sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(request => request.Role)
            .NotEmpty()
            .Must(role => ApplicationRoles.All.Contains(role))
            .WithMessage("Role must be Admin, Developer, or Viewer.");
    }
}
