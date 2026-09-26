using System.ComponentModel;
using Manipulator.Core.Commands;
using Manipulator.Mcp.Runtime;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

/// <summary>
/// One tool per Core command. Each tool parses its arguments, dispatches a single command and
/// reports the result; all of the scene logic stays in Core.
/// </summary>
[McpServerToolType]
public sealed class SceneWriteTools(SceneRun run)
{
    [McpServerTool(Name = "add_entity", Destructive = false, Idempotent = false)]
    [Description(ToolDescriptions.AddEntity)]
    public string AddEntity(
        [Description(ToolDescriptions.GeometryParam)] string geometry,
        [Description(ToolDescriptions.PositionParam)] float[]? position = null,
        [Description(ToolDescriptions.RotationParam)] float[]? rotation = null,
        [Description(ToolDescriptions.ScaleParam)] float[]? scale = null,
        [Description(ToolDescriptions.ColorParam)] string? color = null,
        [Description(ToolDescriptions.OpacityParam)] float? opacity = null,
        [Description(ToolDescriptions.MetalnessParam)] float? metalness = null,
        [Description(ToolDescriptions.RoughnessParam)] float? roughness = null,
        [Description(ToolDescriptions.NameParam)] string? name = null
    )
    {
        return run.Invoke(
            "add_entity",
            ToolJson.Args(
                ("geometry", ToolJson.Value(geometry)),
                ("position", ToolJson.Vector(position)),
                ("rotation", ToolJson.Vector(rotation)),
                ("scale", ToolJson.Vector(scale)),
                ("color", ToolJson.Value(color)),
                ("opacity", ToolJson.Value(opacity)),
                ("metalness", ToolJson.Value(metalness)),
                ("roughness", ToolJson.Value(roughness)),
                ("name", ToolJson.Value(name))
            ),
            run =>
            {
                if (!ToolArgs.TryGeometry(geometry, out var geometryType, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryVector(position, "position", out var positionValue, out error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryVector(rotation, "rotation", out var rotationValue, out error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryVector(scale, "scale", out var scaleValue, out error))
                    return ToolOutcome.Failure(error!);

                var result = run.Dispatcher.Dispatch(
                    new AddEntityCommand(
                        Geometry: geometryType,
                        Position: positionValue,
                        Rotation: rotationValue,
                        Scale: scaleValue,
                        Color: color,
                        Opacity: opacity,
                        Metalness: metalness,
                        Roughness: roughness,
                        Name: name
                    )
                );

                return ToolResults.From(run, result, result.Data as string);
            }
        );
    }

    [McpServerTool(Name = "move_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.MoveEntity)]
    public string MoveEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.RequiredPositionParam)] float[] position
    )
    {
        return run.Invoke(
            "move_entity",
            ToolJson.Args(
                ("entityId", ToolJson.Value(entityId)),
                ("position", ToolJson.Vector(position))
            ),
            run =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryRequiredVector(position, "position", out var value, out error))
                    return ToolOutcome.Failure(error!);

                var result = run.Dispatcher.Dispatch(new MoveEntityCommand(id, value));
                return ToolResults.From(run, result, id);
            }
        );
    }

    [McpServerTool(Name = "rotate_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.RotateEntity)]
    public string RotateEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.RequiredRotationParam)] float[] rotation
    )
    {
        return run.Invoke(
            "rotate_entity",
            ToolJson.Args(
                ("entityId", ToolJson.Value(entityId)),
                ("rotation", ToolJson.Vector(rotation))
            ),
            run =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryRequiredVector(rotation, "rotation", out var value, out error))
                    return ToolOutcome.Failure(error!);

                var result = run.Dispatcher.Dispatch(new RotateEntityCommand(id, value));
                return ToolResults.From(run, result, id);
            }
        );
    }

    [McpServerTool(Name = "scale_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.ScaleEntity)]
    public string ScaleEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.RequiredScaleParam)] float[] scale
    )
    {
        return run.Invoke(
            "scale_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId)), ("scale", ToolJson.Vector(scale))),
            run =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryRequiredVector(scale, "scale", out var value, out error))
                    return ToolOutcome.Failure(error!);

                var result = run.Dispatcher.Dispatch(new ScaleEntityCommand(id, value));
                return ToolResults.From(run, result, id);
            }
        );
    }

    [McpServerTool(Name = "set_material", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.SetMaterial)]
    public string SetMaterial(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.MaterialColorParam)] string? color = null,
        [Description(ToolDescriptions.MaterialOpacityParam)] float? opacity = null,
        [Description(ToolDescriptions.MaterialMetalnessParam)] float? metalness = null,
        [Description(ToolDescriptions.MaterialRoughnessParam)] float? roughness = null
    )
    {
        return run.Invoke(
            "set_material",
            ToolJson.Args(
                ("entityId", ToolJson.Value(entityId)),
                ("color", ToolJson.Value(color)),
                ("opacity", ToolJson.Value(opacity)),
                ("metalness", ToolJson.Value(metalness)),
                ("roughness", ToolJson.Value(roughness))
            ),
            run =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var result = run.Dispatcher.Dispatch(
                    new SetMaterialCommand(id, color, opacity, metalness, roughness)
                );
                return ToolResults.From(run, result, id);
            }
        );
    }

    [McpServerTool(Name = "rename_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.RenameEntity)]
    public string RenameEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.NewNameParam)] string name
    )
    {
        return run.Invoke(
            "rename_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId)), ("name", ToolJson.Value(name))),
            run =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var result = run.Dispatcher.Dispatch(new RenameEntityCommand(id, name ?? ""));
                return ToolResults.From(run, result, id);
            }
        );
    }

    [McpServerTool(Name = "remove_entity", Destructive = true, Idempotent = false)]
    [Description(ToolDescriptions.RemoveEntity)]
    public string RemoveEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId
    )
    {
        return run.Invoke(
            "remove_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId))),
            run =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var result = run.Dispatcher.Dispatch(new RemoveEntityCommand(id));
                return ToolResults.From(run, result, id);
            }
        );
    }
}
