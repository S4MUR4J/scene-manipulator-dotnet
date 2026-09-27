using FluentValidation;
using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Contracts;

namespace Manipulator.Scenarios.Validation;

class RoleSelectorDtoValidator : AbstractValidator<RoleSelectorDto>
{
    public RoleSelectorDtoValidator()
    {
        RuleFor(x => x)
            .Custom(
                (dto, context) =>
                {
                    var name = RoleName(context);

                    if (dto.NameContains is not { Count: > 0 })
                        context.AddFailure(
                            $"Role '{name}': 'name_contains' must list at least one keyword."
                        );

                    if (dto.Count is not > 0)
                        context.AddFailure($"'roles.{name}.count' must be a positive integer.");

                    if (dto.Geometry is null)
                        return;

                    foreach (var g in dto.Geometry)
                    {
                        if (!Enum.TryParse<GeometryType>(g, ignoreCase: true, out _))
                            context.AddFailure(
                                $"Role '{name}': unknown geometry '{g}'. Valid values: "
                                    + string.Join(", ", Enum.GetNames<GeometryType>())
                            );
                    }
                }
            );
    }

    private static string RoleName(ValidationContext<RoleSelectorDto> context) =>
        context.RootContextData.TryGetValue("RoleName", out var name) ? (string)name! : "role";
}
