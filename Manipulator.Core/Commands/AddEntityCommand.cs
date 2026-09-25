using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

public record AddEntityCommand(
    GeometryType? Geometry = null,
    Vector3? Position = null,
    Vector3? Rotation = null,
    Vector3? Scale = null,
    string? Color = null,
    float? Opacity = null,
    float? Metalness = null,
    float? Roughness = null,
    string? Name = null,
    long? ExpectedVersion = null
) : ICommand
{
    public string Type => "AddEntity";
}
