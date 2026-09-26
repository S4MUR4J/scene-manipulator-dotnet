using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Serialization;
using Manipulator.Mcp.Runtime;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

[McpServerToolType]
public sealed class SceneReadTools(SceneRun run)
{
    [McpServerTool(Name = "get_scene", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetScene)]
    public string GetScene()
    {
        return run.Invoke(
            "get_scene",
            ToolJson.Args(),
            run =>
                ToolOutcome.Success(
                    new JsonObject
                    {
                        ["scene"] = JsonNode.Parse(SceneSerializer.Serialize(run.Scene)),
                    }
                )
        );
    }

    [McpServerTool(Name = "get_entity", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetEntity)]
    public string GetEntity([Description(ToolDescriptions.EntityIdParam)] string entityId)
    {
        return run.Invoke(
            "get_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId))),
            run =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var entity = run.Scene.GetEntity(id);
                if (entity is null)
                    return ToolOutcome.Failure($"Entity '{id}' does not exist.");

                return ToolOutcome.Success(
                    new JsonObject
                    {
                        ["entity"] = JsonNode.Parse(SceneSerializer.SerializeEntity(entity)),
                    }
                );
            }
        );
    }
}
