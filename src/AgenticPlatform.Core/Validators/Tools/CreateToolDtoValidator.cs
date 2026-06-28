using AgenticPlatform.Core.DTOs.Tools;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Tools;

public sealed class CreateToolDtoValidator : AbstractValidator<CreateToolDto>
{
    public CreateToolDtoValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.Category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.InputSchemaJson)
            .NotEmpty()
            .Must(JsonValidator.BeValidJson)
            .WithMessage("InputSchemaJson must be valid JSON.");

        RuleFor(request => request.EndpointUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeValidAbsoluteUrl)
            .WithMessage("EndpointUrl must be a valid absolute URL.");
    }

    private static bool BeValidAbsoluteUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
