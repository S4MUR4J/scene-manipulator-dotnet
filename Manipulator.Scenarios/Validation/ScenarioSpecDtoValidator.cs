using FluentValidation;
using FluentValidation.Results;
using Manipulator.Scenarios.Contracts;

namespace Manipulator.Scenarios.Validation;

class ScenarioSpecDtoValidator : AbstractValidator<ScenarioSpecDto>
{
    private ScenarioSpecDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Missing required field 'id'.");
        RuleFor(x => x.Category).NotEmpty().WithMessage("Missing required field 'category'.");
        RuleFor(x => x.Prompt).NotEmpty().WithMessage("Missing required field 'prompt'.");

        RuleFor(x => x.Roles)
            .Custom(
                (roles, context) =>
                {
                    if (roles is not { Count: > 0 })
                    {
                        context.AddFailure("'roles' must declare at least one role.");
                        return;
                    }

                    var validator = new RoleSelectorDtoValidator();
                    foreach (var (name, role) in roles)
                    {
                        var childContext = new ValidationContext<RoleSelectorDto>(role)
                        {
                            RootContextData = { ["RoleName"] = name },
                        };
                        foreach (var failure in validator.Validate(childContext).Errors)
                            context.AddFailure(failure.ErrorMessage);
                    }
                }
            );

        RuleFor(x => x.Requirements)
            .Custom(
                (requirements, context) =>
                {
                    if (requirements is not { Count: > 0 })
                    {
                        context.AddFailure("'requirements' must list at least one requirement.");
                        return;
                    }

                    var seenIds = new HashSet<string>();
                    foreach (var requirement in requirements)
                    {
                        if (
                            !string.IsNullOrWhiteSpace(requirement.Id)
                            && !seenIds.Add(requirement.Id)
                        )
                            context.AddFailure($"Duplicate requirement id '{requirement.Id}'.");
                    }
                }
            );

        RuleForEach(x => x.Requirements).SetValidator(new RequirementDtoValidator());

        RuleFor(x => x.IterationLimit)
            .Must(v => v is > 0)
            .WithMessage("'iteration_limit' must be a positive integer.");

        RuleFor(x => x.TimeoutS)
            .Must(v => v is > 0)
            .WithMessage("'timeout_s' must be a positive integer.");

        RuleFor(x => x.Runs).NotNull().WithMessage("'runs' is required.");

        RuleFor(x => x.Runs!)
            .SetValidator(new RunConfigDtoValidator())
            .When(x => x.Runs is not null);
    }

    public static ValidationResult ValidateSpec(ScenarioSpecDto dto)
    {
        var context = new ValidationContext<ScenarioSpecDto>(dto)
        {
            RootContextData =
            {
                ["RoleNames"] = new HashSet<string>(dto.Roles?.Keys ?? Enumerable.Empty<string>()),
            },
        };
        return new ScenarioSpecDtoValidator().Validate(context);
    }
}
