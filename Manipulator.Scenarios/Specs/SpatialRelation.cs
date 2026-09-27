namespace Manipulator.Scenarios.Specs;

/// <summary>
/// Named spatial relations a <see cref="SpatialRelationRequirement"/> can check, each with its
/// own tolerance parameters on <see cref="SpatialRelationParams"/>. Not every relation is wired
/// up by an existing scenario spec yet (e.g. LeftOf, RightOf, SymmetricAround, Facing) — they're
/// named here because the spec format needs to express them per the scenario description
/// template, and scenarios 2+ will use them.
/// </summary>
public enum SpatialRelation
{
    OnTopOf,
    Between,
    Near,
    LeftOf,
    RightOf,
    SymmetricAround,
    Facing,
    NoOverlap,
    ScaleOrder,
}
