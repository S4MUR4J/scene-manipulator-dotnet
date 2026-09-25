using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Serialization;
using Manipulator.Mcp.Session;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

/// <summary>
/// The perception half of the loop: the agent reads the scene, decides, then acts. Reads publish no
/// scene events, so the call log is what records that they happened.
/// </summary>
[McpServerToolType]
public sealed class SceneReadTools(ToolGateway gateway)
{
    [McpServerTool(Name = "get_scene", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetScene)]
    public string GetScene()
    {
        return gateway.Execute(
            "get_scene",
            ToolJson.Args(),
            session =>
                ToolOutcome.Success(
                    new JsonObject
                    {
                        ["scene"] = JsonNode.Parse(SceneSerializer.Serialize(session.Scene)),
                    }
                )
        );
    }

    [McpServerTool(Name = "get_entity", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetEntity)]
    public string GetEntity([Description(ToolDescriptions.EntityIdParam)] string entityId)
    {
        return gateway.Execute(
            "get_entity",
            ToolJson.Args(("entityId", ToolJson.Value(entityId))),
            session =>
            {
                if (!ToolArgs.TryEntityId(entityId, out var id, out var error))
                    return ToolOutcome.Failure(error!);

                var entity = session.Scene.GetEntity(id);
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
