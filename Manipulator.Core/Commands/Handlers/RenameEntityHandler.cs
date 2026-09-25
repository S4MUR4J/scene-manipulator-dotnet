using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands.Handlers;

public class RenameEntityHandler : ICommandHandler<RenameEntityCommand>
{
    public CommandResult Handle(Scene scene, RenameEntityCommand command)
    {
        var entity = scene.GetEntity(command.EntityId);
        if (entity is null)
            return CommandResult.Fail($"Entity '{command.EntityId}' does not exist.");

        var oldName = entity.Get<EntityName>() ?? new EntityName();
        var newName = new EntityName(command.Name);
        entity.Set(newName);

        return CommandResult.Ok([
            new ComponentChangedEvent(
                EntityId: command.EntityId,
                ComponentType: nameof(EntityName),
                NewValue: newName,
                OldValue: oldName
            ),
        ]);
    }
}
