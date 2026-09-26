using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Mcp.Logging;

namespace Manipulator.Mcp.Runtime;

/// <summary>
/// The one agent run this process serves: an in-memory <see cref="Scene"/>, the dispatcher that
/// mutates it and the log of everything the agent did to it. Nothing is persisted — the scene lives
/// and dies with the process.
/// </summary>
/// <remarks>
/// There is deliberately no session layer. The streamable HTTP transport is stateless from protocol
/// revision 2026-07-28 onwards — the <c>Mcp-Session-Id</c> header is gone and a tool call carries
/// nothing that identifies a client — so one agent works against one run and the runner gets
/// isolation by starting a server per run. A finished run can never leak into the next one.
/// </remarks>
public sealed class SceneRun
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

    public SceneRun(RunOptions options)
    {
        var startingScene = StartingScene.Load(options);

        Id = options.RunId;
        Scene = startingScene.Scene;
        EventBus = new EventBus();
        Dispatcher = CommandDispatcherFactory.Create(Scene, EventBus);
        CallLog = new CallLog(Id, options.CallLogPath);
        StartedAt = DateTimeOffset.UtcNow;

        EventBus.Subscribe<ISceneEvent>(OnSceneEvent);

        var data = new JsonObject
        {
            ["starting_scene"] = startingScene.Path,
            ["entity_count"] = Scene.Count,
        };

        // Dropped components and fields in the starting scene are the scenario's problem, not the
        // agent's: record them so a run is never scored against a scene that silently lost parts.
        if (startingScene.Warnings.Count > 0)
            data["starting_scene_warnings"] = new JsonArray(
                [.. startingScene.Warnings.Select(warning => JsonValue.Create(warning))]
            );

        CallLog.Append(
            new CallLogEntry
            {
                RunId = Id,
                Kind = CallLogEntry.RunKind,
                Name = "run_started",
                Ok = true,
                SceneVersionBefore = Scene.Version,
                SceneVersionAfter = Scene.Version,
                Data = data,
            }
        );
    }

    public string Id { get; }

    public Scene Scene { get; }

    public EventBus EventBus { get; }

    public CommandDispatcher Dispatcher { get; }

    public CallLog CallLog { get; }

    public DateTimeOffset StartedAt { get; }

    public bool IsFinished { get; private set; }

    public long ToolCallCount => _callIndex;

    /// <summary>
    /// Runs one tool body under the run lock, logs the call with its arguments, result and scene
    /// version delta, and renders the JSON the agent sees.
    /// </summary>
    /// <remarks>
    /// Every tool goes through here rather than touching the scene directly: the lock (Kestrel
    /// serves calls concurrently and <see cref="Scene"/> is not thread-safe), the finished check,
    /// the log entry and the result envelope are the same for all ten tools.
    /// </remarks>
    public string Invoke(string toolName, JsonNode? arguments, Func<SceneRun, ToolOutcome> body)
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
                    "Run is already finished; no further tool calls are accepted."
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

            CallLog.Append(
                new CallLogEntry
                {
                    RunId = Id,
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
                RunId = Id,
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
