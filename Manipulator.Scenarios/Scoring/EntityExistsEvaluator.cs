using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

public static class EntityExistsEvaluator
{
    public static RequirementResult Evaluate(
        EntityExistsRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    )
    {
        var failures = new List<string>();
        foreach (var role in requirement.Roles)
        {
            var expected = spec.Roles[role].Count;
            var actual = RoleResolver.Resolve(scene, spec.Roles, role).Count;
            if (actual != expected)
                failures.Add($"role '{role}': expected {expected}, found {actual}");
        }

        return failures.Count == 0
            ? new RequirementResult(
                requirement.Id,
                Passed: true,
                Reason: "All roles matched expected counts."
            )
            : new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: string.Join("; ", failures)
            );
    }
}
