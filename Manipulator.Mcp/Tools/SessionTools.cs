using System.ComponentModel;
using System.Text.Json.Nodes;
using Manipulator.Core.Serialization;
using Manipulator.Mcp.Logging;
using Manipulator.Mcp.Session;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

/// <summary>
/// Gives every run an explicit end. Without it a run has no defined stopping point, and the time
/// and iteration metrics have nothing to measure against.
/// </summary>
[McpServerToolType]
public sealed class SessionTools(ToolGateway gateway)
{
    [McpServerTool(Name = "finish", Destructive = false, Idempotent = false)]
    [Description(ToolDescriptions.Finish)]
    public string Finish()
    {
        return gateway.Execute(
            "finish",
            ToolJson.Args(),
            session =>
            {
                session.MarkFinished();

                var entries = session.CallLog.Entries;
                var toolCalls = entries.Count(entry => entry.Kind == CallLogEntry.ToolCallKind);
                var failedCalls = entries.Count(entry =>
                    entry.Kind == CallLogEntry.ToolCallKind && entry.Ok == false
                );
                var sceneEvents = entries.Count(entry => entry.Kind == CallLogEntry.EventKind);

                var summary = new JsonObject
                {
                    ["scene_version"] = session.Scene.Version,
                    ["entity_count"] = session.Scene.Count,
                    // The finish call itself is logged after this body runs, so count it here.
                    ["tool_calls"] = toolCalls + 1,
                    ["failed_tool_calls"] = failedCalls,
                    ["scene_events"] = sceneEvents,
                    ["duration_ms"] = (DateTimeOffset.UtcNow - session.StartedAt).TotalMilliseconds,
                };

                session.CallLog.Append(
                    new CallLogEntry
                    {
                        SessionId = session.Id,
                        Kind = CallLogEntry.SessionKind,
                        Name = "session_finished",
                        Ok = true,
                        SceneVersionBefore = session.Scene.Version,
                        SceneVersionAfter = session.Scene.Version,
                        Data = new JsonObject
                        {
                            ["summary"] = summary.DeepClone(),
                            ["scene"] = JsonNode.Parse(SceneSerializer.Serialize(session.Scene)),
                        },
                    }
                );

                return ToolOutcome.Success(summary);
            }
        );
    }
}
