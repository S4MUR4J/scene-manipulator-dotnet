namespace Manipulator.Core.Commands;

public record RenameEntityCommand(string EntityId, string Name, long? ExpectedVersion = null)
    : IEntityTargetCommand
{
    public string Type => "RenameEntity";
}
