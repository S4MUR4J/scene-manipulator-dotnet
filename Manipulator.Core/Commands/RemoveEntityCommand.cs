namespace Manipulator.Core.Commands;

public record RemoveEntityCommand(string EntityId, long? ExpectedVersion = null)
    : IEntityTargetCommand
{
    public string Type => "RemoveEntity";
}
