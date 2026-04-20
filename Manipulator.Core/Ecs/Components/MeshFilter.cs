namespace Manipulator.Core.Ecs.Components;

public record MeshFilter(GeometryType Geometry, IReadOnlyDictionary<string, object>? Parameters)
    : IComponent
{
    public string Type => nameof(MeshFilter);
}
