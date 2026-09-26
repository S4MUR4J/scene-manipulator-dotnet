using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Serialization;
using Manipulator.Mcp.Logging;
using Manipulator.Mcp.Runtime;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

/// <summary>
/// Gives every run an explicit end. Without it a run has no defined stopping point, and the time
/// and iteration metrics have nothing to measure against.
/// </summary>
[McpServerToolType]
public sealed class RunTools(SceneRun run)
{
    [McpServerTool(Name = "finish", Destructive = false, Idempotent = false)]
    [Description(ToolDescriptions.Finish)]
    public string Finish()
    {
        return run.Invoke(
            "finish",
            ToolJson.Args(),
            run =>
            {
                run.MarkFinished();

                var entries = run.CallLog.Entries;
                var toolCalls = entries.Count(entry => entry.Kind == CallLogEntry.ToolCallKind);
                var failedCalls = entries.Count(entry =>
                    entry.Kind == CallLogEntry.ToolCallKind && entry.Ok == false
                );
                var sceneEvents = entries.Count(entry => entry.Kind == CallLogEntry.EventKind);

                var summary = new JsonObject
                {
                    ["scene_version"] = run.Scene.Version,
                    ["entity_count"] = run.Scene.Count,
                    // The finish call itself is logged after this body runs, so count it here.
                    ["tool_calls"] = toolCalls + 1,
                    ["failed_tool_calls"] = failedCalls,
                    ["scene_events"] = sceneEvents,
                    ["duration_ms"] = (DateTimeOffset.UtcNow - run.StartedAt).TotalMilliseconds,
                };

                run.CallLog.Append(
                    new CallLogEntry
                    {
                        RunId = run.Id,
                        Kind = CallLogEntry.RunKind,
                        Name = "run_finished",
                        Ok = true,
                        SceneVersionBefore = run.Scene.Version,
                        SceneVersionAfter = run.Scene.Version,
                        Data = new JsonObject
                        {
                            ["summary"] = summary.DeepClone(),
                            ["scene"] = JsonNode.Parse(SceneSerializer.Serialize(run.Scene)),
                        },
                    }
                );

                return ToolOutcome.Success(summary);
            }
        );
    }
}
