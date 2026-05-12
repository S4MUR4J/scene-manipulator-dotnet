namespace Manipulator.Core.Commands;

public record RemoveEntityCommand(string EntityId) : ICommand
{
    public string Type => "RemoveEntity";
}
