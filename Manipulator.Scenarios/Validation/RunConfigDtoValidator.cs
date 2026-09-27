using FluentValidation;
using Manipulator.Scenarios.Contracts;

namespace Manipulator.Scenarios.Validation;

class RunConfigDtoValidator : AbstractValidator<RunConfigDto>
{
    public RunConfigDtoValidator()
    {
        RuleFor(x => x.Main)
            .Must(main => main is > 0)
            .WithMessage("'runs.main' must be a positive integer.");

        RuleFor(x => x.PerVariant)
            .Must(perVariant => perVariant is > 0)
            .WithMessage("'runs.per_variant' must be a positive integer.");
    }
}
