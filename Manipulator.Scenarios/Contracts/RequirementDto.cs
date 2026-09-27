using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Contracts;

record RequirementDto(
    string? Id,
    string? Type,
    List<string>? Roles,
    string? Role,
    string? Component,
    string? Field,
    ComponentValueConstraintDto? Constraint,
    string? Relation,
    List<string>? Subjects,
    List<string>? References,
    SpatialRelationParamsDto? Params
)
{
    public Requirement ToDomain() =>
        Type switch
        {
            "entity_exists" => new EntityExistsRequirement(Id!, Roles!),
            "component_value" => new ComponentValueRequirement(
                Id!,
                Role!,
                Component!,
                Field!,
                Constraint!.ToDomain()
            ),
            "spatial_relation" => new SpatialRelationRequirement(
                Id!,
                RelationNameMap.Values[Relation!],
                Subjects ?? [],
                References ?? [],
                SpatialRelationParamsDto.ToDomain(Params)
            ),
            _ => throw new InvalidOperationException(
                $"Requirement '{Id}' has an unvalidated type '{Type}'."
            ),
        };
}
