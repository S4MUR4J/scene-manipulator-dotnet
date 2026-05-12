using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

public record MoveEntityCommand(string EntityId, Vector3 Position) : ICommand
{
    public string Type => "MoveEntity";
}
