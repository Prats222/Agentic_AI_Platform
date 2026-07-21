using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Tools;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.Tools;

public sealed class UpdateToolDtoValidator : AbstractValidator<UpdateToolDto>
{
    public UpdateToolDtoValidator()
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

        RuleFor(request => request.SecretJson)
            .Must(value => value is null || JsonValidator.BeValidJson(value))
            .WithMessage("SecretJson must be valid JSON.");

        RuleFor(request => request.EndpointUrl)
            .NotEmpty()
            .Must((request, endpointUrl) => endpointUrl.Length <= GetMaximumEndpointLength(request.Category))
            .WithMessage(request => request.Category.Equals(BuiltInToolCategories.PythonScript, StringComparison.OrdinalIgnoreCase)
                ? "Python scripts must be 65535 characters or fewer."
                : "EndpointUrl must be 2048 characters or fewer.")
            .Must((request, endpointUrl) => BeValidEndpoint(request.Category, endpointUrl))
            .WithMessage("EndpointUrl must be a valid absolute HTTP/HTTPS URL, or a builtin:// URL for built-in tools.");
    }

    private static int GetMaximumEndpointLength(string category)
    {
        return category.Equals(BuiltInToolCategories.PythonScript, StringComparison.OrdinalIgnoreCase) ? 65535 : 2048;
    }

    private static bool BeValidEndpoint(string category, string value)
    {
        if (category.Equals(BuiltInToolCategories.PythonScript, StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        return !BuiltInToolCategories.IsHttpCategory(category)
            && Uri.TryCreate(value, UriKind.Absolute, out uri)
            && uri.Scheme.Equals("builtin", StringComparison.OrdinalIgnoreCase);
    }
}
