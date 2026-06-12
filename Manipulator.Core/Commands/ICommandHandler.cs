using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

public interface ICommandHandler<in T>
    where T : ICommand
{
    CommandResult Handle(Scene scene, T command);
}
