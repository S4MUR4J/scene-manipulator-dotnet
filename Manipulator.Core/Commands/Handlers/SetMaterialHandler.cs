using FluentValidation;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Ecs.Components.Validators;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands.Handlers;

public class SetMaterialHandler : ICommandHandler<SetMaterialCommand>
{
    private static readonly MeshRendererValidator Validator = new MeshRendererValidator();

    public CommandResult Handle(Scene scene, SetMaterialCommand command)
    {
        var entity = scene.GetEntity(command.EntityId);
        if (entity is null)
            return CommandResult.Fail($"Entity '{command.EntityId}' does not exist.");

        var oldMaterial = entity.Get<MeshRenderer>() ?? new MeshRenderer();
        var newMaterial = oldMaterial with
        {
            Color = command.Color ?? oldMaterial.Color,
            Opacity = command.Opacity ?? oldMaterial.Opacity,
            Metalness = command.Metalness ?? oldMaterial.Metalness,
            Roughness = command.Roughness ?? oldMaterial.Roughness,
        };

        var validation = Validator.Validate(newMaterial);
        if (!validation.IsValid)
            return CommandResult.Fail(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            );

        entity.Set(newMaterial);

        return CommandResult.Ok([
            new ComponentChangedEvent(
                EntityId: command.EntityId,
                ComponentType: nameof(MeshRenderer),
                NewValue: newMaterial,
                OldValue: oldMaterial
            ),
        ]);
    }
}
