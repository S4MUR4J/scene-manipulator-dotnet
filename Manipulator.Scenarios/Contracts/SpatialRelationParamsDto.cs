using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Contracts;

record SpatialRelationParamsDto(
    double? MaxDistanceM,
    double? MinSeparationM,
    double? MarginM,
    double? ToleranceM,
    double? ToleranceDeg,
    string? Plane,
    List<List<string>>? Order,
    string? Metric
)
{
    public static SpatialRelationParams ToDomain(SpatialRelationParamsDto? dto) =>
        new SpatialRelationParams(
            MaxDistanceM: dto?.MaxDistanceM,
            MinSeparationM: dto?.MinSeparationM,
            MarginM: dto?.MarginM,
            ToleranceM: dto?.ToleranceM,
            ToleranceDeg: dto?.ToleranceDeg,
            Plane: dto?.Plane,
            Order: dto?.Order,
            Metric: dto?.Metric
        );
}
