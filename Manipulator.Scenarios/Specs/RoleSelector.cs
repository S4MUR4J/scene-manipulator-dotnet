using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;

namespace Manipulator.Scenarios.Specs;

/// <summary>
/// Names an entity's role in a scenario (e.g. "sofa") and the rule for recognizing it in an
/// agent-produced scene. Agent-generated entity ids are unpredictable, so matching goes by
/// <see cref="EntityName"/> (case-insensitive substring against <see cref="NameContains"/>),
/// optionally narrowed by <see cref="Geometry"/> when the name alone is ambiguous.
///
/// Matching rule when several entities satisfy a role: every matching entity must be checked
/// (an "all" quantifier) rather than picking one arbitrarily — this mirrors prose like "both
/// stools near the table". <see cref="Count"/> is the expected number of matches; a different
/// actual count is itself a failure of the role's <c>entity_exists</c> requirement, and any
/// other requirement referencing an under- or over-populated role is evaluated against
/// whatever actually matched (left to the validator, not this spec).
/// </summary>
public sealed record RoleSelector(
    IReadOnlyList<string> NameContains,
    IReadOnlyList<GeometryType>? Geometry,
    int Count
)
{
    public bool Matches(Entity entity)
    {
        var name = entity.Get<EntityName>()?.Value;
        if (string.IsNullOrEmpty(name))
            return false;

        var nameMatches = NameContains.Any(keyword =>
            name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        );
        if (!nameMatches)
            return false;

        if (Geometry is null)
            return true;

        var geometry = entity.Get<MeshFilter>()?.Geometry;
        return geometry is not null && Geometry.Contains(geometry.Value);
    }
}
