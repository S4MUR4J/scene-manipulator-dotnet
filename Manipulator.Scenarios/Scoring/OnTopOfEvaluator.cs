using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

/// <summary>
/// Passes when every subject entity rests on some reference entity: their world-space AABBs
/// overlap on the XZ plane, and the subject's bottom sits within Params.ToleranceM of the
/// reference's top.
/// </summary>
public static class OnTopOfEvaluator
{
    public static RequirementResult Evaluate(
        SpatialRelationRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    )
    {
        var subjects = RoleResolver.ResolveAll(scene, spec.Roles, requirement.Subjects);
        var references = RoleResolver.ResolveAll(scene, spec.Roles, requirement.References);
        var toleranceM = requirement.Params.ToleranceM!.Value;

        var failures = subjects
            .Select(subject => CheckSubject(subject, references, toleranceM))
            .Where(reason => reason is not null)
            .ToList();

        return failures.Count == 0
            ? new RequirementResult(
                requirement.Id,
                Passed: true,
                Reason: "Every subject rests on a reference within tolerance."
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
        double toleranceM
    )
    {
        var subjectBounds = WorldBounds.Of(subject);
        if (subjectBounds is null)
            return $"entity '{subject.Id}' has no Transform/MeshFilter to compute bounds";

        var overlapping = references
            .Select(reference => (reference.Id, Bounds: WorldBounds.Of(reference)))
            .Where(r => r.Bounds is not null && OverlapsXZ(subjectBounds.Value, r.Bounds!.Value))
            .ToList();

        if (overlapping.Count == 0)
            return $"entity '{subject.Id}' has no horizontal overlap with any reference";

        var (nearestId, gap) = overlapping
            .Select(r => (r.Id, Gap: Math.Abs(subjectBounds.Value.Min.Y - r.Bounds!.Value.Max.Y)))
            .OrderBy(r => r.Gap)
            .First();

        return gap <= toleranceM
            ? null
            : $"entity '{subject.Id}' vertical gap to nearest reference '{nearestId}' is {gap:F3}m, expected <= {toleranceM}m";
    }

    private static bool OverlapsXZ((Vector3 Min, Vector3 Max) a, (Vector3 Min, Vector3 Max) b) =>
        a.Min.X <= b.Max.X && a.Max.X >= b.Min.X && a.Min.Z <= b.Max.Z && a.Max.Z >= b.Min.Z;
}
