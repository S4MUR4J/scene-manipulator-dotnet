using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

/// <summary>Checking only adjacent groups is sufficient since &lt;= is transitive across the chain.</summary>
public static class ScaleOrderEvaluator
{
    public static RequirementResult Evaluate(
        SpatialRelationRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    )
    {
        var groups = requirement
            .Params.Order!.Select(roleNames =>
                RoleResolver.ResolveAll(scene, spec.Roles, roleNames)
            )
            .ToList();

        var failures = new List<string>();
        for (var i = 0; i < groups.Count - 1; i++)
        {
            var reason = CheckAdjacentGroups(groups[i], groups[i + 1]);
            if (reason is not null)
                failures.Add(reason);
        }

        return failures.Count == 0
            ? new RequirementResult(
                requirement.Id,
                Passed: true,
                Reason: "Every group's volume is at most the next group's volume."
            )
            : new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: string.Join("; ", failures)
            );
    }

    private static string? CheckAdjacentGroups(
        IReadOnlyList<Entity> smaller,
        IReadOnlyList<Entity> larger
    )
    {
        // An empty group has nothing to compare; entity_exists already reports the missing role.
        if (smaller.Count == 0 || larger.Count == 0)
            return null;

        var smallerVolumes = new List<(string Id, double Volume)>();
        foreach (var entity in smaller)
        {
            var volume = AabbVolume(entity);
            if (volume is null)
                return $"entity '{entity.Id}' has no Transform/MeshFilter to compute volume";
            smallerVolumes.Add((entity.Id, volume.Value));
        }

        var largerVolumes = new List<(string Id, double Volume)>();
        foreach (var entity in larger)
        {
            var volume = AabbVolume(entity);
            if (volume is null)
                return $"entity '{entity.Id}' has no Transform/MeshFilter to compute volume";
            largerVolumes.Add((entity.Id, volume.Value));
        }

        var (maxId, maxVolume) = smallerVolumes.OrderByDescending(v => v.Volume).First();
        var (minId, minVolume) = largerVolumes.OrderBy(v => v.Volume).First();

        return maxVolume <= minVolume
            ? null
            : $"entity '{maxId}' (volume {maxVolume:F4}) is larger than '{minId}' (volume {minVolume:F4}), "
                + "expected the earlier group's volumes to not exceed the later group's";
    }

    private static double? AabbVolume(Entity entity)
    {
        var transform = entity.Get<Transform>();
        var meshFilter = entity.Get<MeshFilter>();
        if (transform is null || meshFilter is null)
            return null;

        var size = GeometryBounds.For(meshFilter.Geometry) * transform.Scale;
        return (double)(size.X * size.Y * size.Z);
    }
}
