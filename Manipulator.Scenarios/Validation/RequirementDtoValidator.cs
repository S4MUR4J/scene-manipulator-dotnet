using FluentValidation;
using Manipulator.Scenarios.Contracts;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Validation;

class RequirementDtoValidator : AbstractValidator<RequirementDto>
{
    public RequirementDtoValidator()
    {
        RuleFor(x => x).Custom(Validate);
    }

    private static void Validate(RequirementDto dto, ValidationContext<RequirementDto> context)
    {
        var reqId = dto.Id;

        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            context.AddFailure($"Missing required field 'requirements[{reqId}].type'.");
            return;
        }

        switch (dto.Type)
        {
            case "entity_exists":
                ValidateEntityExists(dto, context, reqId);
                break;
            case "component_value":
                ValidateComponentValue(dto, context, reqId);
                break;
            case "spatial_relation":
                ValidateSpatialRelation(dto, context, reqId);
                break;
            default:
                context.AddFailure(
                    $"Requirement '{reqId}': unknown type '{dto.Type}'. "
                        + "Valid values: entity_exists, component_value, spatial_relation."
                );
                break;
        }
    }

    private static void ValidateEntityExists(
        RequirementDto dto,
        ValidationContext<RequirementDto> context,
        string? reqId
    )
    {
        if (dto.Roles is not { Count: > 0 })
        {
            context.AddFailure(
                $"Requirement '{reqId}' (entity_exists): 'roles' must list at least one role."
            );
            return;
        }

        foreach (var role in dto.Roles)
            RequireRole(context, reqId, role);
    }

    private static void ValidateComponentValue(
        RequirementDto dto,
        ValidationContext<RequirementDto> context,
        string? reqId
    )
    {
        if (string.IsNullOrWhiteSpace(dto.Role))
            context.AddFailure($"Missing required field 'requirements[{reqId}].role'.");
        else
            RequireRole(context, reqId, dto.Role);

        if (string.IsNullOrWhiteSpace(dto.Component))
            context.AddFailure($"Missing required field 'requirements[{reqId}].component'.");

        if (string.IsNullOrWhiteSpace(dto.Field))
            context.AddFailure($"Missing required field 'requirements[{reqId}].field'.");

        if (dto.Constraint is null)
        {
            context.AddFailure($"Requirement '{reqId}' (component_value): missing 'constraint'.");
            return;
        }

        if (!dto.Constraint.HasBounds)
            context.AddFailure(
                $"Requirement '{reqId}' (component_value): 'constraint' has no bounds set."
            );
    }

    private static void ValidateSpatialRelation(
        RequirementDto dto,
        ValidationContext<RequirementDto> context,
        string? reqId
    )
    {
        if (string.IsNullOrWhiteSpace(dto.Relation))
        {
            context.AddFailure($"Missing required field 'requirements[{reqId}].relation'.");
            return;
        }

        if (!RelationNameMap.Values.TryGetValue(dto.Relation, out var relation))
        {
            context.AddFailure(
                $"Requirement '{reqId}': unknown relation '{dto.Relation}'. "
                    + "Valid values: "
                    + string.Join(", ", RelationNameMap.Values.Keys)
            );
            return;
        }

        var subjects = dto.Subjects ?? [];
        var references = dto.References ?? [];
        var parameters = dto.Params;

        foreach (var role in subjects.Concat(references))
            RequireRole(context, reqId, role);

        switch (relation)
        {
            case SpatialRelation.OnTopOf:
                RequireNonEmpty(context, reqId, "subjects", subjects);
                RequireNonEmpty(context, reqId, "references", references);
                RequirePositiveParam(context, reqId, "tolerance_m", parameters?.ToleranceM);
                break;
            case SpatialRelation.Between:
                RequireNonEmpty(context, reqId, "subjects", subjects);
                if (references.Count != 2)
                    context.AddFailure(
                        $"Requirement '{reqId}' (between): 'references' must list exactly 2 roles, got {references.Count}."
                    );
                RequireNonNegativeParam(context, reqId, "margin_m", parameters?.MarginM);
                break;
            case SpatialRelation.Near:
                RequireNonEmpty(context, reqId, "subjects", subjects);
                RequireNonEmpty(context, reqId, "references", references);
                RequirePositiveParam(context, reqId, "max_distance_m", parameters?.MaxDistanceM);
                break;
            case SpatialRelation.NoOverlap:
                if (subjects.Count < 2)
                    context.AddFailure(
                        $"Requirement '{reqId}' (no_overlap): 'subjects' must list at least 2 roles."
                    );
                RequireNonNegativeParam(
                    context,
                    reqId,
                    "min_separation_m",
                    parameters?.MinSeparationM
                );
                break;
            case SpatialRelation.ScaleOrder:
                var order = parameters?.Order;
                if (order is null || order.Count < 2)
                {
                    context.AddFailure(
                        $"Requirement '{reqId}' (scale_order): 'params.order' must list at least 2 role groups."
                    );
                }
                else
                {
                    foreach (var group in order)
                    {
                        if (group.Count == 0)
                        {
                            context.AddFailure(
                                $"Requirement '{reqId}' (scale_order): 'params.order' contains an empty group."
                            );
                            continue;
                        }
                        foreach (var role in group)
                            RequireRole(context, reqId, role);
                    }
                }
                if (parameters?.Metric != "aabb_volume")
                    context.AddFailure(
                        $"Requirement '{reqId}' (scale_order): 'params.metric' must be 'aabb_volume'."
                    );
                break;
            default:
                // Not yet used by any spec; only the roles referenced above are validated.
                break;
        }
    }

    private static void RequireRole(
        ValidationContext<RequirementDto> context,
        string? reqId,
        string role
    )
    {
        var roleNames = (HashSet<string>)context.RootContextData["RoleNames"];
        if (!roleNames.Contains(role))
            context.AddFailure($"Requirement '{reqId}' references undeclared role '{role}'.");
    }

    private static void RequireNonEmpty(
        ValidationContext<RequirementDto> context,
        string? reqId,
        string field,
        List<string> values
    )
    {
        if (values.Count == 0)
            context.AddFailure($"Requirement '{reqId}': '{field}' must be non-empty.");
    }

    private static void RequirePositiveParam(
        ValidationContext<RequirementDto> context,
        string? reqId,
        string field,
        double? value
    )
    {
        if (value is null || value <= 0)
            context.AddFailure(
                $"Requirement '{reqId}': 'params.{field}' must be a positive number."
            );
    }

    private static void RequireNonNegativeParam(
        ValidationContext<RequirementDto> context,
        string? reqId,
        string field,
        double? value
    )
    {
        if (value is null || value < 0)
            context.AddFailure(
                $"Requirement '{reqId}': 'params.{field}' must be a non-negative number."
            );
    }
}
