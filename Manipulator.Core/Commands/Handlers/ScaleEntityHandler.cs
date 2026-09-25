using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands.Handlers;

public class ScaleEntityHandler : ICommandHandler<ScaleEntityCommand>
{
    public CommandResult Handle(Scene scene, ScaleEntityCommand command)
    {
        if (!command.Scale.IsFinite())
            return CommandResult.Fail("Scale contains NaN or Infinity.");

        if (command.Scale.X <= 0 || command.Scale.Y <= 0 || command.Scale.Z <= 0)
            return CommandResult.Fail(
                $"Scale must be positive on every axis, got ({command.Scale.X}, {command.Scale.Y}, {command.Scale.Z})."
            );

        var entity = scene.GetEntity(command.EntityId);
        if (entity is null)
            return CommandResult.Fail($"Entity '{command.EntityId}' does not exist.");

        var oldTransform = entity.Get<Transform>() ?? new Transform();
        var newTransform = oldTransform with { Scale = command.Scale };
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
