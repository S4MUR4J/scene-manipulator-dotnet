using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Commands.Validation;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Core.IdGeneration;

namespace Manipulator.Core.Commands;

/// <summary>
/// Builds a <see cref="CommandDispatcher"/> with every command, handler and validator registered.
/// The MCP server, DSL and text adapters all go through this so they share one command set.
/// </summary>
public static class CommandDispatcherFactory
{
    public static CommandDispatcher Create(
        Scene scene,
        EventBus eventBus,
        IGuidGenerator? guidGenerator = null
    )
    {
        var dispatcher = new CommandDispatcher(scene, eventBus);
        var versionConflict = new VersionConflictValidator();
        var entityExists = new EntityExistsValidator();

        dispatcher.Register(
            "AddEntity",
            new AddEntityHandler(guidGenerator ?? new GuidGenerator()),
            versionConflict
        );
        dispatcher.Register("MoveEntity", new MoveEntityHandler(), versionConflict, entityExists);
        dispatcher.Register(
            "RemoveEntity",
            new RemoveEntityHandler(),
            versionConflict,
            entityExists
        );
        dispatcher.Register(
            "RotateEntity",
            new RotateEntityHandler(),
            versionConflict,
            entityExists
        );
        dispatcher.Register("ScaleEntity", new ScaleEntityHandler(), versionConflict, entityExists);
        dispatcher.Register("SetMaterial", new SetMaterialHandler(), versionConflict, entityExists);
        dispatcher.Register(
            "RenameEntity",
            new RenameEntityHandler(),
            versionConflict,
            entityExists
        );

        return dispatcher;
    }
}
