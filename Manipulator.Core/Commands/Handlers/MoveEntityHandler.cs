using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands.Handlers;

public class MoveEntityHandler : ICommandHandler<MoveEntityCommand>
{
    public CommandResult Handle(Scene scene, MoveEntityCommand command)
    {
        var entity = scene.GetEntity(command.EntityId);
        if (entity is null)
            return CommandResult.Fail($"Entity '{command.EntityId}' does not exist.");

        var oldTransform = entity.Get<Transform>() ?? new Transform();
        var newTransform = oldTransform with { Position = command.Position };
        entity.Set(newTransform);

        return CommandResult.Ok(
            [
                new ComponentChangedEvent(
                    EntityId: command.EntityId,
                    ComponentType: nameof(Transform),
                    NewValue: newTransform,
                    OldValue: oldTransform
                )
            ]
        );
    }
}
