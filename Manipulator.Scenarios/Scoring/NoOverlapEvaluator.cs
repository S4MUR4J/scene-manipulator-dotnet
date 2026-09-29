using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

public static class NoOverlapEvaluator
{
    public static RequirementResult Evaluate(
        SpatialRelationRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    )
    {
        var subjects = RoleResolver.ResolveAll(scene, spec.Roles, requirement.Subjects);
        var minSeparationM = requirement.Params.MinSeparationM!.Value;

        var failures = new List<string>();
        for (var i = 0; i < subjects.Count; i++)
        for (var j = i + 1; j < subjects.Count; j++)
        {
            var reason = CheckPair(subjects[i], subjects[j], minSeparationM);
            if (reason is not null)
                failures.Add(reason);
        }

        return failures.Count == 0
            ? new RequirementResult(
                requirement.Id,
                Passed: true,
                Reason: "No pair of subjects is closer than the minimum separation."
            )
            : new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: string.Join("; ", failures)
            );
    }

    private static string? CheckPair(Entity a, Entity b, double minSeparationM)
    {
        var boundsA = WorldBounds.Of(a);
        var boundsB = WorldBounds.Of(b);
        if (boundsA is null || boundsB is null)
            return $"entity '{a.Id}' or '{b.Id}' has no Transform/MeshFilter to compute bounds";

        var separation = SeparationDistance(boundsA.Value, boundsB.Value);
        return separation >= minSeparationM
            ? null
            : $"entities '{a.Id}' and '{b.Id}' are {separation:F3}m apart, expected >= {minSeparationM}m";
    }

    private static double SeparationDistance(
        (Vector3 Min, Vector3 Max) a,
        (Vector3 Min, Vector3 Max) b
    )
    {
        var gapX = Math.Max(0f, Math.Max(a.Min.X, b.Min.X) - Math.Min(a.Max.X, b.Max.X));
        var gapY = Math.Max(0f, Math.Max(a.Min.Y, b.Min.Y) - Math.Min(a.Max.Y, b.Max.Y));
        var gapZ = Math.Max(0f, Math.Max(a.Min.Z, b.Min.Z) - Math.Min(a.Max.Z, b.Max.Z));
        return Math.Sqrt(gapX * gapX + gapY * gapY + gapZ * gapZ);
    }
}
