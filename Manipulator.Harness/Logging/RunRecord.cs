using System.Text.Json.Nodes;

namespace Manipulator.Harness.Logging;

public sealed record RunConfigLog(
    string Scenario,
    string? Variant,
    string Approach,
    string Model,
    int? Seed,
    int RunIndex
);

public sealed record ToolErrorLog(string Tool, string Message, DateTimeOffset At);

public sealed record ToolCallLog(string Tool, JsonNode? Arguments, bool IsError, string? Error);

public sealed record StepRecord(
    string RunId,
    int Iteration,
    int InputTokens,
    int OutputTokens,
    string StopReason,
    IReadOnlyList<ToolCallLog> ToolCalls
)
{
    public string RecordType => "step";
}

public sealed record RunRecord(
    string RunId,
    RunConfigLog Config,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    double DurationMs,
    string StopReason,
    string? FatalError,
    int LlmCalls,
    int InputTokens,
    int OutputTokens,
    int ToolCallsTotal,
    IReadOnlyDictionary<string, int> ToolCallsByTool,
    int SceneReads,
    int SceneWrites,
    IReadOnlyList<ToolErrorLog> ToolErrors,
    JsonNode? FinalScene
)
{
    public string RecordType => "run";
}
