using AgenticPlatform.Core.DTOs.AI;
using FluentValidation;

namespace AgenticPlatform.Core.Validators.AI;

public sealed class TestLLMProviderDtoValidator : AbstractValidator<TestLLMProviderDto>
{
    public TestLLMProviderDtoValidator()
    {
        RuleFor(request => request.Prompt)
            .NotEmpty()
            .MaximumLength(8000);
    }
}
