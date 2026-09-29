using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

public static class SpatialRelationEvaluator
{
    public static RequirementResult Evaluate(
        SpatialRelationRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    ) =>
        requirement.Relation switch
        {
            SpatialRelation.OnTopOf => OnTopOfEvaluator.Evaluate(requirement, spec, scene),
            SpatialRelation.Near => NearEvaluator.Evaluate(requirement, spec, scene),
            SpatialRelation.Between => BetweenEvaluator.Evaluate(requirement, spec, scene),
            SpatialRelation.NoOverlap => NoOverlapEvaluator.Evaluate(requirement, spec, scene),
            SpatialRelation.ScaleOrder => ScaleOrderEvaluator.Evaluate(requirement, spec, scene),
            _ => throw new NotSupportedException(
                $"Requirement '{requirement.Id}': spatial relation '{requirement.Relation}' is not yet implemented."
            ),
        };
}
