using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

public record AddEntityCommand(
    GeometryType? Geometry = null,
    Vector3? Position = null,
    string? Name = null,
    long? ExpectedVersion = null
) : ICommand
{
    public string Type => "AddEntity";
}
