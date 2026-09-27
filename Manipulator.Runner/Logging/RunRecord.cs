namespace Manipulator.Runner.Logging;

public sealed record RunConfigLog(
    string Scenario,
    string? Variant,
    string Approach,
    string ModelName,
    string Provider,
    string Model,
    int? Seed,
    int RunIndex
);

public sealed record ToolErrorLog(string Tool, string Message, DateTimeOffset At);

/// <summary>Arguments are logged as raw JSON text, not parsed - keeps this a plain log record.</summary>
public sealed record ToolCallLog(string Tool, string ArgumentsJson, bool IsError, string? Error);

public sealed record StepRecord(
    string RunId,
    long Iteration,
    long InputTokens,
    long OutputTokens,
    string StopReason,
    IReadOnlyList<ToolCallLog> ToolCalls
);

/// <summary>FinalSceneJson is the raw plain-scene-format JSON text (also saved as a standalone file).</summary>
public sealed record RunRecord(
    string RunId,
    RunConfigLog Config,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    double DurationMs,
    string StopReason,
    string? FatalError,
    long LlmCalls,
    long InputTokens,
    long OutputTokens,
    int ToolCallsTotal,
    IReadOnlyDictionary<string, int> ToolCallsByTool,
    int SceneReads,
    int SceneWrites,
    IReadOnlyList<ToolErrorLog> ToolErrors,
    string FinalSceneJson
);
