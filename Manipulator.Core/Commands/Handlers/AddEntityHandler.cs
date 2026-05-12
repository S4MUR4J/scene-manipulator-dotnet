using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.IdGeneration;

namespace Manipulator.Core.Commands.Handlers;

public class AddEntityHandler(IGuidGenerator guidGenerator) : ICommandHandler<AddEntityCommand>
{
    public CommandResult Handle(Scene scene, AddEntityCommand command)
    {
        var id = guidGenerator.Generate();
        var entity = new Entity(id);

        entity.Set(new Transform { Position = command.Position ?? Vector3.Zero });
        entity.Set(new MeshFilter(command.Geometry ?? GeometryType.Cube, null));
        entity.Set(new MeshRenderer());
        entity.Set(new EntityName(command.Name ?? string.Empty));

        scene.AddEntity(entity);

        var events = new[] { new EntityAddedEvent(entity) };
        return CommandResult.Ok(events, data: id);
    }
}
