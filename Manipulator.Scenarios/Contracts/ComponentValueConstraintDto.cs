using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Contracts;

record ComponentValueConstraintDto(
    string? ExpectedValue,
    double? Min,
    double? Max,
    double? HueMinDeg,
    double? HueMaxDeg
)
{
    public bool HasBounds =>
        ExpectedValue is not null
        || Min is not null
        || Max is not null
        || HueMinDeg is not null
        || HueMaxDeg is not null;

    public ComponentValueConstraint ToDomain() =>
        new ComponentValueConstraint(ExpectedValue, Min, Max, HueMinDeg, HueMaxDeg);
}
