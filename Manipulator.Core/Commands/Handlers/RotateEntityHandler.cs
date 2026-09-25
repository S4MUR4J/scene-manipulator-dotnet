using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands.Handlers;

public class RotateEntityHandler : ICommandHandler<RotateEntityCommand>
{
    public CommandResult Handle(Scene scene, RotateEntityCommand command)
    {
        if (!command.Rotation.IsFinite())
            return CommandResult.Fail("Rotation contains NaN or Infinity.");

        var entity = scene.GetEntity(command.EntityId);
        if (entity is null)
            return CommandResult.Fail($"Entity '{command.EntityId}' does not exist.");

        var oldTransform = entity.Get<Transform>() ?? new Transform();
        var newTransform = oldTransform with { Rotation = command.Rotation };
        entity.Set(newTransform);

        return CommandResult.Ok([
            new ComponentChangedEvent(
                EntityId: command.EntityId,
                ComponentType: nameof(Transform),
                NewValue: newTransform,
                OldValue: oldTransform
            ),
        ]);
    }
}
