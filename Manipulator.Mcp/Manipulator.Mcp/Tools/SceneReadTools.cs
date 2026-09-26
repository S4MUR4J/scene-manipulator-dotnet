using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Ecs;
using Manipulator.Core.Results;
using Manipulator.Core.Serialization;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

[McpServerToolType]
public sealed class SceneReadTools(Scene scene)
{
    [McpServerTool(Name = "get_scene", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetScene)]
    public string GetScene()
    {
        var data = new JsonObject { ["scene"] = JsonNode.Parse(SceneSerializer.Serialize(scene)) };
        return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Success(data));
    }

    [McpServerTool(Name = "get_entity", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetEntity)]
    public string GetEntity([Description(ToolDescriptions.EntityIdParam)] string entityId)
    {
        if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
            return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error!));

        var entity = scene.GetEntity(id);
        if (entity is null)
            return McpJsonSerializer.Serialize(
                ManipulatorResult<JsonObject?>.Failure($"Entity '{id}' does not exist.", code: 404)
            );

        var data = new JsonObject
        {
            ["entity"] = JsonNode.Parse(SceneSerializer.SerializeEntity(entity)),
        };
        return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Success(data));
    }
}
