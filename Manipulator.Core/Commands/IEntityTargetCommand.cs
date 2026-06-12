namespace Manipulator.Core.Commands;

public interface IEntityTargetCommand : ICommand
{
    string EntityId { get; }
}
