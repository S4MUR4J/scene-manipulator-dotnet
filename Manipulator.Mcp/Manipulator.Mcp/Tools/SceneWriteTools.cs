using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Results;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

[McpServerToolType]
public sealed class SceneWriteTools(Scene scene, CommandDispatcher dispatcher)
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
        if (
            !ToolArgs.TryGeometry(geometry, out var geometryType, out var error)
            || !ToolArgs.TryVector(position, "position", out var positionValue, out error)
            || !ToolArgs.TryVector(rotation, "rotation", out var rotationValue, out error)
            || !ToolArgs.TryVector(scale, "scale", out var scaleValue, out error)
        )
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var result = dispatcher.Dispatch(
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

        return McpJsonSerializer.Serialize(
            ManipulatorResult<JsonObject?>.FromCommandResult(
                result,
                MutationData(result.Data as string)
            )
        );
    }

    [McpServerTool(Name = "move_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.MoveEntity)]
    public string MoveEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.RequiredPositionParam)] float[] position
    )
    {
        if (
            !ToolArgs.TryEntityId(entityId, out var id, out var error)
            || !ToolArgs.TryRequiredVector(position, "position", out var value, out error)
        )
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var result = dispatcher.Dispatch(new MoveEntityCommand(id, value));
        return McpJsonSerializer.Serialize(
            ManipulatorResult<JsonObject?>.FromCommandResult(result, MutationData(id))
        );
    }

    [McpServerTool(Name = "rotate_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.RotateEntity)]
    public string RotateEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.RequiredRotationParam)] float[] rotation
    )
    {
        if (
            !ToolArgs.TryEntityId(entityId, out var id, out var error)
            || !ToolArgs.TryRequiredVector(rotation, "rotation", out var value, out error)
        )
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var result = dispatcher.Dispatch(new RotateEntityCommand(id, value));
        return McpJsonSerializer.Serialize(
            ManipulatorResult<JsonObject?>.FromCommandResult(result, MutationData(id))
        );
    }

    [McpServerTool(Name = "scale_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.ScaleEntity)]
    public string ScaleEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.RequiredScaleParam)] float[] scale
    )
    {
        if (
            !ToolArgs.TryEntityId(entityId, out var id, out var error)
            || !ToolArgs.TryRequiredVector(scale, "scale", out var value, out error)
        )
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var result = dispatcher.Dispatch(new ScaleEntityCommand(id, value));
        return McpJsonSerializer.Serialize(
            ManipulatorResult<JsonObject?>.FromCommandResult(result, MutationData(id))
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
        if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var result = dispatcher.Dispatch(
            new SetMaterialCommand(id, color, opacity, metalness, roughness)
        );
        return McpJsonSerializer.Serialize(
            ManipulatorResult<JsonObject?>.FromCommandResult(result, MutationData(id))
        );
    }

    [McpServerTool(Name = "rename_entity", Destructive = false, Idempotent = true)]
    [Description(ToolDescriptions.RenameEntity)]
    public string RenameEntity(
        [Description(ToolDescriptions.EntityIdParam)] string entityId,
        [Description(ToolDescriptions.NewNameParam)] string? name
    )
    {
        if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var result = dispatcher.Dispatch(new RenameEntityCommand(id, name ?? ""));
        return McpJsonSerializer.Serialize(
            ManipulatorResult<JsonObject?>.FromCommandResult(result, MutationData(id))
        );
    }

    [McpServerTool(Name = "remove_entity", Destructive = true, Idempotent = false)]
    [Description(ToolDescriptions.RemoveEntity)]
    public string RemoveEntity([Description(ToolDescriptions.EntityIdParam)] string entityId)
    {
        if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var result = dispatcher.Dispatch(new RemoveEntityCommand(id));
        return McpJsonSerializer.Serialize(
            ManipulatorResult<JsonObject?>.FromCommandResult(result, MutationData(id))
        );
    }

    private JsonObject MutationData(string? entityId)
    {
        var data = new JsonObject { ["scene_version"] = scene.Version };
        if (entityId is not null)
            data["entity_id"] = entityId;

        return data;
    }
}
