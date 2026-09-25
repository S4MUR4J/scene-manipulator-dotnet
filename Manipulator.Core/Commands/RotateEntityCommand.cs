using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

public record RotateEntityCommand(string EntityId, Vector3 Rotation, long? ExpectedVersion = null)
    : IEntityTargetCommand
{
    public string Type => "RotateEntity";
}
