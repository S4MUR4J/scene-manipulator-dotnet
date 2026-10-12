using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

/// <summary>
/// Uses entity centers, not bounding boxes - the spec format has no other param to define a
/// box-based "between" for this relation.
/// </summary>
public static class BetweenEvaluator
{
    public static RequirementResult Evaluate(
        SpatialRelationRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    )
    {
        var subjects = RoleResolver.ResolveAll(scene, spec.Roles, requirement.Subjects);
        var references = RoleResolver.ResolveAll(scene, spec.Roles, requirement.References);
        var marginM = requirement.Params.MarginM!.Value;

        if (references.Count < 2)
            return new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: $"expected two reference entities, found {references.Count}"
            );

        var a = references[0];
        var b = references[1];

        var failures = subjects
            .Select(subject => CheckSubject(subject, a, b, marginM))
            .Where(reason => reason is not null)
            .ToList();

        return failures.Count == 0
            ? new RequirementResult(
                requirement.Id,
                Passed: true,
                Reason: "Every subject lies between the two references."
            )
            : new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: string.Join("; ", failures)
            );
    }

    private static string? CheckSubject(Entity subject, Entity a, Entity b, double marginM)
    {
        var subjectPos = subject.Get<Transform>()?.Position;
        var posA = a.Get<Transform>()?.Position;
        var posB = b.Get<Transform>()?.Position;
        if (subjectPos is null || posA is null || posB is null)
            return $"entity '{subject.Id}' or a reference has no Transform to compute position";

        var ab = posB.Value - posA.Value;
        var lengthSquared = ab.X * ab.X + ab.Y * ab.Y + ab.Z * ab.Z;
        if (lengthSquared == 0)
            return $"references '{a.Id}' and '{b.Id}' are at the same position; 'between' is undefined";

        var ap = subjectPos.Value - posA.Value;
        var dot = ap.X * ab.X + ap.Y * ab.Y + ap.Z * ab.Z;
        var length = Math.Sqrt(lengthSquared);
        var projectionM = dot / length;

        if (projectionM < -marginM)
            return $"entity '{subject.Id}' is {-projectionM:F3}m before reference '{a.Id}', expected within {marginM}m margin";
        if (projectionM > length + marginM)
            return $"entity '{subject.Id}' is {projectionM - length:F3}m beyond reference '{b.Id}', expected within {marginM}m margin";
        return null;
    }
}
