using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

public record ScaleEntityCommand(string EntityId, Vector3 Scale, long? ExpectedVersion = null)
    : IEntityTargetCommand
{
    public string Type => "ScaleEntity";
}
