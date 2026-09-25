using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Mcp.Logging;

namespace Manipulator.Mcp.Session;

/// <summary>
/// One agent run: an in-memory <see cref="Scene"/>, the dispatcher that mutates it and the log of
/// everything the agent did to it. Nothing is persisted — the scene lives and dies with the session.
/// </summary>
public sealed class SceneSession
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        // Relaxed escaping keeps quotes and apostrophes literal in what the agent reads: escape
        // sequences cost tokens and make error messages harder to follow.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Lock _gate = new Lock();
    private long _callIndex;

    public SceneSession(string id, StartingScene startingScene, string? callLogPath)
    {
        Id = id;
        Scene = startingScene.Create();
        EventBus = new EventBus();
        Dispatcher = CommandDispatcherFactory.Create(Scene, EventBus);
        CallLog = new CallLog(id, callLogPath);
        StartedAt = DateTimeOffset.UtcNow;
        LastActivityAt = StartedAt;

        EventBus.Subscribe<ISceneEvent>(OnSceneEvent);

        CallLog.Append(
            new CallLogEntry
            {
                SessionId = id,
                Kind = CallLogEntry.SessionKind,
                Name = "session_started",
                Ok = true,
                SceneVersionBefore = Scene.Version,
                SceneVersionAfter = Scene.Version,
                Data = new JsonObject
                {
                    ["starting_scene"] = startingScene.Path,
                    ["entity_count"] = Scene.Count,
                },
            }
        );
    }

    public string Id { get; }

    public Scene Scene { get; }

    public EventBus EventBus { get; }

    public CommandDispatcher Dispatcher { get; }

    public CallLog CallLog { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public bool IsFinished { get; private set; }

    public long ToolCallCount => _callIndex;

    /// <summary>
    /// Runs one tool body under the session lock, logs the call with its arguments, result and
    /// scene version delta, and renders the JSON the agent sees.
    /// </summary>
    public string Invoke(string toolName, JsonNode? arguments, Func<SceneSession, ToolOutcome> body)
    {
        lock (_gate)
        {
            var callIndex = ++_callIndex;
            var versionBefore = Scene.Version;
            var startedAt = Stopwatch.GetTimestamp();

            ToolOutcome outcome;
            if (IsFinished)
            {
                outcome = ToolOutcome.Failure(
                    "Session is already finished; no further tool calls are accepted."
                );
            }
            else
            {
                try
                {
                    outcome = body(this);
                }
                catch (Exception ex)
                {
                    outcome = ToolOutcome.Failure($"{ex.GetType().Name}: {ex.Message}");
                }
            }

            LastActivityAt = DateTimeOffset.UtcNow;
            CallLog.Append(
                new CallLogEntry
                {
                    SessionId = Id,
                    CallIndex = callIndex,
                    Kind = CallLogEntry.ToolCallKind,
                    Name = toolName,
                    Arguments = arguments,
                    Ok = outcome.Ok,
                    Error = outcome.Error,
                    DurationMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                    SceneVersionBefore = versionBefore,
                    SceneVersionAfter = Scene.Version,
                    Data = outcome.Data?.DeepClone(),
                }
            );

            return Render(outcome);
        }
    }

    internal void MarkFinished()
    {
        IsFinished = true;
    }

    private static string Render(ToolOutcome outcome)
    {
        var node = new JsonObject { ["ok"] = outcome.Ok };
        if (!outcome.Ok)
            node["error"] = outcome.Error;

        if (outcome.Data is not null)
        {
            foreach (var (key, value) in outcome.Data)
                node[key] = value?.DeepClone();
        }

        return node.ToJsonString(JsonOptions);
    }

    private void OnSceneEvent(ISceneEvent sceneEvent)
    {
        var data = sceneEvent switch
        {
            EntityAddedEvent added => new JsonObject { ["entity_id"] = added.Entity.Id },
            EntityRemovedEvent removed => new JsonObject { ["entity_id"] = removed.EntityId },
            ComponentChangedEvent changed => new JsonObject
            {
                ["entity_id"] = changed.EntityId,
                ["component_type"] = changed.ComponentType,
            },
            var _ => new JsonObject(),
        };

        CallLog.Append(
            new CallLogEntry
            {
                SessionId = Id,
                CallIndex = _callIndex,
                Kind = CallLogEntry.EventKind,
                Name = sceneEvent.GetType().Name,
                SceneVersionBefore = Scene.Version,
                SceneVersionAfter = Scene.Version,
                Data = data,
            }
        );
    }
}
