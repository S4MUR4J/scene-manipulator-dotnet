using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands.Handlers;

/// <summary>
/// Holds a submitted scene to the same rules as <see cref="AddEntityHandler"/>, so the text
/// approach can't build scenes the command-based approach would reject: every entity needs a
/// mesh_filter and a positive scale, and missing transform, mesh_renderer or entity_name
/// components are filled with the defaults add_entity would use. Each fill is reported as a
/// warning in <see cref="CommandResult.Data"/>.
/// </summary>
public class ReplaceSceneHandler : ICommandHandler<ReplaceSceneCommand>
{
    public CommandResult Handle(Scene scene, ReplaceSceneCommand command)
    {
        var entities = command.Scene.Entities.Values.ToList();

        foreach (var entity in entities)
        {
            var error = Validate(entity);
            if (error is not null)
                return CommandResult.Fail(error);
        }

        var warnings = new List<string>();
        foreach (var entity in entities)
            FillMissingComponents(entity, warnings);

        // One submission is one write, so the version moves by one regardless of entity count.
        var nextVersion = scene.Version + 1;
        scene.Clear();
        foreach (var entity in entities)
            scene.AddEntity(entity);
        scene.RestoreVersion(nextVersion);

        var events = new[] { new SceneReplacedEvent(entities.Select(e => e.Id).ToList()) };
        return CommandResult.Ok(events, data: warnings);
    }

    private static string? Validate(Entity entity)
    {
        if (!entity.Has<MeshFilter>())
            return $"Entity '{entity.Id}' is missing required component 'mesh_filter'.";

        var scale = entity.Get<Transform>()?.Scale;
        if (scale is { } s && (s.X <= 0 || s.Y <= 0 || s.Z <= 0))
            return $"Entity '{entity.Id}', component 'transform': scale must be positive on every "
                + $"axis, got ({s.X}, {s.Y}, {s.Z}).";

        return null;
    }

    private static void FillMissingComponents(Entity entity, List<string> warnings)
    {
        if (!entity.Has<Transform>())
        {
            entity.Set(new Transform());
            warnings.Add(MissingComponentWarning(entity, "transform"));
        }

        if (!entity.Has<MeshRenderer>())
        {
            entity.Set(new MeshRenderer());
            warnings.Add(MissingComponentWarning(entity, "mesh_renderer"));
        }

        if (!entity.Has<EntityName>())
        {
            entity.Set(new EntityName());
            warnings.Add(MissingComponentWarning(entity, "entity_name"));
        }
    }

    private static string MissingComponentWarning(Entity entity, string component) =>
        $"Entity '{entity.Id}': missing component '{component}' was filled with defaults.";
}
