using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

/// <summary>
/// Passes when every subject entity is within Params.MaxDistanceM of some reference entity,
/// measuring center-to-center distance on Params.Plane ("xz", "xy", or full 3D when unset).
/// </summary>
public static class NearEvaluator
{
    public static RequirementResult Evaluate(
        SpatialRelationRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    )
    {
        var subjects = RoleResolver.ResolveAll(scene, spec.Roles, requirement.Subjects);
        var references = RoleResolver.ResolveAll(scene, spec.Roles, requirement.References);
        var maxDistanceM = requirement.Params.MaxDistanceM!.Value;
        var plane = requirement.Params.Plane;

        var failures = subjects
            .Select(subject => CheckSubject(subject, references, maxDistanceM, plane))
            .Where(reason => reason is not null)
            .ToList();

        return failures.Count == 0
            ? new RequirementResult(
                requirement.Id,
                Passed: true,
                Reason: "Every subject is near a reference."
            )
            : new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: string.Join("; ", failures)
            );
    }

    private static string? CheckSubject(
        Entity subject,
        IReadOnlyList<Entity> references,
        double maxDistanceM,
        string? plane
    )
    {
        var position = subject.Get<Transform>()?.Position;
        if (position is null)
            return $"entity '{subject.Id}' has no Transform to compute position";

        var nearest = references
            .Select(reference =>
                (reference.Id, Position: reference.Get<Transform>()?.Position)
            )
            .Where(r => r.Position is not null)
            .Select(r => (r.Id, Distance: Distance(position.Value, r.Position!.Value, plane)))
            .OrderBy(r => r.Distance)
            .Cast<(string Id, double Distance)?>()
            .FirstOrDefault();

        if (nearest is null)
            return $"entity '{subject.Id}' has no reference with a Transform to measure distance to";

        return nearest.Value.Distance <= maxDistanceM
            ? null
            : $"entity '{subject.Id}' is {nearest.Value.Distance:F3}m from nearest reference "
                + $"'{nearest.Value.Id}', expected <= {maxDistanceM}m";
    }

    private static double Distance(Vector3 a, Vector3 b, string? plane)
    {
        var d = a - b;
        return plane switch
        {
            "xz" => Math.Sqrt(d.X * d.X + d.Z * d.Z),
            "xy" => Math.Sqrt(d.X * d.X + d.Y * d.Y),
            _ => Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z),
        };
    }
}
