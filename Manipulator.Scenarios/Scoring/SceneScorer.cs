using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

public static class SceneScorer
{
    public static ScoringResult Score(ScenarioSpec spec, Scene scene)
    {
        var results = spec
            .Requirements.Select(requirement => Evaluate(requirement, spec, scene))
            .ToList();
        return ScoringResult.From(results);
    }

    private static RequirementResult Evaluate(
        Requirement requirement,
        ScenarioSpec spec,
        Scene scene
    ) =>
        requirement switch
        {
            EntityExistsRequirement r => EntityExistsEvaluator.Evaluate(r, spec, scene),
            ComponentValueRequirement r => ComponentValueEvaluator.Evaluate(r, spec, scene),
            SpatialRelationRequirement r => SpatialRelationEvaluator.Evaluate(r, spec, scene),
            _ => throw new NotSupportedException(
                $"Requirement '{requirement.Id}' has unsupported type '{requirement.GetType().Name}'."
            ),
        };
}
