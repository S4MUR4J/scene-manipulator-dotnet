using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Ecs.Components.Validators;
using Manipulator.Core.Events;
using Manipulator.Core.IdGeneration;

namespace Manipulator.Core.Commands.Handlers;

public class AddEntityHandler(IGuidGenerator guidGenerator) : ICommandHandler<AddEntityCommand>
{
    private static readonly MeshFilterValidator FilterValidator = new MeshFilterValidator();
    private static readonly MeshRendererValidator MaterialValidator = new MeshRendererValidator();

    public CommandResult Handle(Scene scene, AddEntityCommand command)
    {
        var position = command.Position ?? Vector3.Zero;
        var rotation = command.Rotation ?? Vector3.Zero;
        var scale = command.Scale ?? Vector3.One;

        if (!position.IsFinite())
            return CommandResult.Fail("Position contains NaN or Infinity.");
        if (!rotation.IsFinite())
            return CommandResult.Fail("Rotation contains NaN or Infinity.");
        if (!scale.IsFinite())
            return CommandResult.Fail("Scale contains NaN or Infinity.");
        if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0)
            return CommandResult.Fail(
                $"Scale must be positive on every axis, got ({scale.X}, {scale.Y}, {scale.Z})."
            );

        var meshFilter = new MeshFilter(command.Geometry ?? GeometryType.Cube, null);
        var filterValidation = FilterValidator.Validate(meshFilter);
        if (!filterValidation.IsValid)
            return CommandResult.Fail(
                string.Join("; ", filterValidation.Errors.Select(e => e.ErrorMessage))
            );

        var material = new MeshRenderer(
            Color: command.Color ?? "#ffffff",
            Opacity: command.Opacity ?? 1.0f,
            Metalness: command.Metalness ?? 0.0f,
            Roughness: command.Roughness ?? 0.5f
        );
        var materialValidation = MaterialValidator.Validate(material);
        if (!materialValidation.IsValid)
            return CommandResult.Fail(
                string.Join("; ", materialValidation.Errors.Select(e => e.ErrorMessage))
            );

        var id = guidGenerator.Generate();
        var entity = new Entity(id);

        entity.Set(
            new Transform
            {
                Position = position,
                Rotation = rotation,
                Scale = scale,
            }
        );
        entity.Set(meshFilter);
        entity.Set(material);
        entity.Set(new EntityName(command.Name ?? string.Empty));

        scene.AddEntity(entity);

        var events = new[] { new EntityAddedEvent(entity) };
        return CommandResult.Ok(events, data: id);
    }
}
