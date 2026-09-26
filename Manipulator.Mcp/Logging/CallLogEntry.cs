using System.Text.Json.Nodes;

namespace Manipulator.Mcp.Logging;

/// <summary>
/// One line of the run log. Tool calls and scene events share the shape so the harness can read the
/// whole run as a single ordered stream.
/// </summary>
public sealed record CallLogEntry
{
    public const string ToolCallKind = "tool_call";
    public const string EventKind = "event";
    public const string RunKind = "run";

    public long Seq { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public required string RunId { get; init; }

    /// <summary>
    /// Index of the tool call this entry belongs to. Scene events carry the index of the call that
    /// produced them, so writes can be attributed even though they are logged before the call
    /// result is known.
    /// </summary>
    public long CallIndex { get; init; }

    /// <summary>One of <see cref="ToolCallKind"/>, <see cref="EventKind"/>, <see cref="RunKind"/>.</summary>
    public required string Kind { get; init; }

    /// <summary>Tool name, event type name, or run lifecycle step.</summary>
    public required string Name { get; init; }

    /// <summary>Arguments exactly as the agent sent them. Null for events.</summary>
    public JsonNode? Arguments { get; init; }

    public bool? Ok { get; init; }

    public string? Error { get; init; }

    public double? DurationMs { get; init; }

    public long SceneVersionBefore { get; init; }

    public long SceneVersionAfter { get; init; }

    public JsonNode? Data { get; init; }
}
