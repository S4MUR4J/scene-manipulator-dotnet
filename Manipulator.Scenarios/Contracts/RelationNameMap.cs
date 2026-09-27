using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Contracts;

static class RelationNameMap
{
    public static readonly IReadOnlyDictionary<string, SpatialRelation> Values = new Dictionary<
        string,
        SpatialRelation
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["on_top_of"] = SpatialRelation.OnTopOf,
        ["between"] = SpatialRelation.Between,
        ["near"] = SpatialRelation.Near,
        ["left_of"] = SpatialRelation.LeftOf,
        ["right_of"] = SpatialRelation.RightOf,
        ["symmetric_around"] = SpatialRelation.SymmetricAround,
        ["facing"] = SpatialRelation.Facing,
        ["no_overlap"] = SpatialRelation.NoOverlap,
        ["scale_order"] = SpatialRelation.ScaleOrder,
    };
}
