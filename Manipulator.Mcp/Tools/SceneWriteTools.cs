using System.ComponentModel;
using Manipulator.Core.Commands;
using Manipulator.Mcp.Session;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

/// <summary>
/// One tool per Core command. Each tool parses its arguments, dispatches a single command and
/// reports the result; all of the scene logic stays in Core.
/// </summary>
[McpServerToolType]
public sealed class SceneWriteTools(ToolGateway gateway)
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
        return gateway.Execute(
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
            session =>
            {
                if (!ToolArgs.TryGeometry(geometry, out var geometryType, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryVector(position, "position", out var positionValue, out error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryVector(rotation, "rotation", out var rotationValue, out error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryVector(scale, "scale", out var scaleValue, out error))
                    return ToolOutcome.Failure(error!);

                var result = session.Dispatcher.Dispatch(
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

                return ToolResults.From(session, result, result.Data as string);
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
        return gateway.Execute(
            "move_entity",
            ToolJson.Args(
                ("entityId", ToolJson.Value(entityId)),
                ("position", ToolJson.Vector(position))
            ),
            session =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryRequiredVector(position, "position", out var value, out error))
                    return ToolOutcome.Failure(error!);

                var result = session.Dispatcher.Dispatch(new MoveEntityCommand(id, value));
                return ToolResults.From(session, result, id);
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
        return gateway.Execute(
            "rotate_entity",
            ToolJson.Args(
                ("entityId", ToolJson.Value(entityId)),
                ("rotation", ToolJson.Vector(rotation))
            ),
            session =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryRequiredVector(rotation, "rotation", out var value, out error))
                    return ToolOutcome.Failure(error!);

                var result = session.Dispatcher.Dispatch(new RotateEntityCommand(id, value));
                return ToolResults.From(session, result, id);
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
        return gateway.Execute(
            "scale_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId)), ("scale", ToolJson.Vector(scale))),
            session =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);
                if (!ToolArgs.TryRequiredVector(scale, "scale", out var value, out error))
                    return ToolOutcome.Failure(error!);

                var result = session.Dispatcher.Dispatch(new ScaleEntityCommand(id, value));
                return ToolResults.From(session, result, id);
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
        return gateway.Execute(
            "set_material",
            ToolJson.Args(
                ("entityId", ToolJson.Value(entityId)),
                ("color", ToolJson.Value(color)),
                ("opacity", ToolJson.Value(opacity)),
                ("metalness", ToolJson.Value(metalness)),
                ("roughness", ToolJson.Value(roughness))
            ),
            session =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var result = session.Dispatcher.Dispatch(
                    new SetMaterialCommand(id, color, opacity, metalness, roughness)
                );
                return ToolResults.From(session, result, id);
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
        return gateway.Execute(
            "rename_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId)), ("name", ToolJson.Value(name))),
            session =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var result = session.Dispatcher.Dispatch(new RenameEntityCommand(id, name ?? ""));
                return ToolResults.From(session, result, id);
            }
        );
    }

    [McpServerTool(Name = "remove_entity", Destructive = true, Idempotent = false)]
    [Description(ToolDescriptions.RemoveEntity)]
    public string RemoveEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId
    )
    {
        return gateway.Execute(
            "remove_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId))),
            session =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var result = session.Dispatcher.Dispatch(new RemoveEntityCommand(id));
                return ToolResults.From(session, result, id);
            }
        );
    }
}
