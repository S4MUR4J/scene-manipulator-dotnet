using Manipulator.Runner.Configuration;
using Manipulator.Runner.Execution;
using Manipulator.Runner.Models;
using Manipulator.Scenarios.Scoring;

namespace Manipulator.Runner.Logging;

public sealed record RunConfigLog(
    string Scenario,
    string? Variant,
    string Approach,
    string ModelName,
    string Provider,
    string Model,
    int? Seed,
    int RunIndex,
    string? Notes = null
)
{
    public static RunConfigLog From(RunConfig config) =>
        new RunConfigLog(
            config.Scenario,
            config.Variant,
            config.Approach,
            config.ModelName,
            config.Provider.ToString(),
            config.Model,
            config.Seed,
            config.RunIndex,
            config.Notes
        );
}

public sealed record ToolErrorLog(string Tool, string Message, DateTimeOffset At);

public sealed record RequirementResultLog(string RequirementId, bool Passed, string Reason)
{
    public static RequirementResultLog From(RequirementResult result) =>
        new RequirementResultLog(result.RequirementId, result.Passed, result.Reason);
}

public sealed record ScoringLog(
    double Coverage,
    bool Success,
    IReadOnlyList<RequirementResultLog> Requirements
)
{
    public static ScoringLog From(ScoringResult result) =>
        new ScoringLog(
            result.Coverage,
            result.Success,
            result.Requirements.Select(RequirementResultLog.From).ToList()
        );
}

/// <summary>Arguments are logged as raw JSON text, not parsed - keeps this a plain log record.</summary>
public sealed record ToolCallLog(string Tool, string ArgumentsJson, bool IsError, string? Error);

public sealed record StepRecord(
    string RunId,
    long Iteration,
    long InputTokens,
    long OutputTokens,
    string StopReason,
    IReadOnlyList<ToolCallLog> ToolCalls
)
{
    public static StepRecord FromResponse(
        string runId,
        long iteration,
        ModelResponse response,
        IReadOnlyList<ToolCallLog> toolCalls
    ) =>
        new StepRecord(
            runId,
            iteration,
            response.InputTokens,
            response.OutputTokens,
            response.StopReason,
            toolCalls
        );
}

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
    string FinalSceneJson,
    ScoringLog? Scoring = null
)
{
    internal static RunRecord FromRun(
        string runId,
        RunConfig config,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        double durationMs,
        LoopResult result,
        RunMetrics metrics,
        string finalSceneJson
    ) =>
        new RunRecord(
            runId,
            RunConfigLog.From(config),
            startTime,
            endTime,
            durationMs,
            result.StopReason.ToString(),
            result.FatalError,
            metrics.LlmCalls,
            metrics.InputTokens,
            metrics.OutputTokens,
            metrics.ToolCallsByTool.Values.Sum(),
            metrics.ToolCallsByTool,
            metrics.SceneReads,
            metrics.SceneWrites,
            metrics.ToolErrors,
            finalSceneJson
        );
}
