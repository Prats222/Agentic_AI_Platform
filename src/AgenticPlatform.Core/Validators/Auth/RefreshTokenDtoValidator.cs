using AgenticPlatform.Core.DTOs.Auth;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Auth;

public sealed class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
