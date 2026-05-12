using Manipulator.Core.Ecs;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands.Handlers;

public class RemoveEntityHandler : ICommandHandler<RemoveEntityCommand>
{
    public CommandResult Handle(Scene scene, RemoveEntityCommand command)
    {
        var removed = scene.RemoveEntity(command.EntityId);

        if (!removed)
            return CommandResult.Fail($"Entity '{command.EntityId}' does not exist.");

        return CommandResult.Ok([new EntityRemovedEvent(command.EntityId)]);
    }
}
