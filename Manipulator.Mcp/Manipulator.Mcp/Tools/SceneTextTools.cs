using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Results;
using Manipulator.Core.Serialization;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

/// <summary>
/// The declarative text approach: the agent reads the whole scene and writes back a whole scene,
/// instead of issuing per-entity commands.
/// </summary>
[McpServerToolType]
public sealed class SceneTextTools(Scene currentScene, CommandDispatcher dispatcher)
{
    [McpServerTool(Name = "get_scene", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetScene)]
    public string GetScene() => SceneReadTools.SceneResponse(currentScene);

    [McpServerTool(Name = "submit_scene", Destructive = true, Idempotent = true)]
    [Description(ToolDescriptions.SubmitScene)]
    public string SubmitScene([Description(ToolDescriptions.SceneParam)] JsonObject scene)
    {
        SceneDeserializationResult submitted;
        try
        {
            submitted = SceneSerializer.Deserialize(scene.ToJsonString());
        }
        catch (SceneDeserializationException ex)
        {
            return Failure(ex.Message);
        }

        var result = dispatcher.Dispatch(new ReplaceSceneCommand(submitted.Scene));
        if (!result.IsSuccess)
            return Failure(result.Error ?? "Command failed.");

        var warnings = submitted.Warnings.Concat((IReadOnlyList<string>)result.Data!);
        var data = new JsonObject
        {
            ["scene_version"] = currentScene.Version,
            ["entity_count"] = currentScene.Count,
            ["warnings"] = new JsonArray([.. warnings.Select(w => (JsonNode)w)]),
        };
        return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Success(data));
    }

    private static string Failure(string error) =>
        McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Failure(error));
}
