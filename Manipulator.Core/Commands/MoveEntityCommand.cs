using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

public record MoveEntityCommand(string EntityId, Vector3 Position, long? ExpectedVersion = null)
    : IEntityTargetCommand
{
    public string Type => "MoveEntity";
}
