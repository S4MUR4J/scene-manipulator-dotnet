namespace Manipulator.Scenarios.Specs;

/// <summary>Base type for anything a validator can score against a produced scene.</summary>
public abstract record Requirement(string Id);

/// <summary>
/// Every role in <see cref="Roles"/> must be matched by exactly its declared
/// <see cref="RoleSelector.Count"/> in the produced scene.
/// </summary>
public sealed record EntityExistsRequirement(string Id, IReadOnlyList<string> Roles)
    : Requirement(Id);

/// <summary>Constrains a single field of a component on the entities matching a role.</summary>
public sealed record ComponentValueRequirement(
    string Id,
    string Role,
    string Component,
    string Field,
    ComponentValueConstraint Constraint
) : Requirement(Id);

/// <summary>
/// Exactly one of <see cref="ExpectedValue"/>, the Min/Max range, or the hue range should be
/// set, depending on the field being constrained (e.g. ExpectedValue for a hex color, Min/Max
/// for a scale value, HueMinDeg/HueMaxDeg for a color's hue).
/// </summary>
public sealed record ComponentValueConstraint(
    string? ExpectedValue = null,
    double? Min = null,
    double? Max = null,
    double? HueMinDeg = null,
    double? HueMaxDeg = null
);

/// <summary>
/// Checks a <see cref="SpatialRelation"/> between the entities matching the <see cref="Subjects"/>
/// roles and (for binary relations) the <see cref="References"/> roles, using the tolerances in
/// <see cref="Params"/>. Every matching entity for every subject role is checked (see
/// <see cref="RoleSelector"/> for the multi-match rule) — e.g. Near with Subjects: ["stool"]
/// requires each stool individually to be near the reference.
/// </summary>
public sealed record SpatialRelationRequirement(
    string Id,
    SpatialRelation Relation,
    IReadOnlyList<string> Subjects,
    IReadOnlyList<string> References,
    SpatialRelationParams Params
) : Requirement(Id);

/// <summary>
/// Tolerance parameters for a <see cref="SpatialRelationRequirement"/>. Which fields apply
/// depends on the relation: e.g. OnTopOf uses ToleranceM, Near uses MaxDistanceM, ScaleOrder
/// uses Order and Metric. Distances are computed on Plane when set ("xz", "xy" or "xyz";
/// null means "xyz").
/// </summary>
public sealed record SpatialRelationParams(
    double? MaxDistanceM = null,
    double? MinSeparationM = null,
    double? MarginM = null,
    double? ToleranceM = null,
    double? ToleranceDeg = null,
    string? Plane = null,
    IReadOnlyList<IReadOnlyList<string>>? Order = null,
    string? Metric = null
);
