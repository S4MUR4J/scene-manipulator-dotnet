using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Ecs;
using Manipulator.Core.Results;
using Manipulator.Mcp.Tools;
using ModelContextProtocol.Server;

namespace Manipulator.Runner.Mcp;

[McpServerToolType]
sealed class FinishTool(Scene scene, ScenarioRunState runState)
{
    private const string Description =
        "Call this when the scene is complete and there is nothing left to do. It ends the run; "
        + "further tool calls are still accepted, but the run is recorded as finished.";

    [McpServerTool(Name = "finish", Destructive = false, Idempotent = false)]
    [Description(Description)]
    public string Finish()
    {
        runState.MarkFinished();

        var data = new JsonObject
        {
            ["scene_version"] = scene.Version,
            ["entity_count"] = scene.Count,
            ["duration_ms"] = (DateTimeOffset.UtcNow - runState.StartedAt).TotalMilliseconds,
        };

        return McpJsonSerializer.Serialize(ManipulatorResult<JsonObject?>.Success(data));
    }
}
