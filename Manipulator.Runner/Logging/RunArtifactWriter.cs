using Manipulator.Runner.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;

namespace Manipulator.Runner.Logging;

/// <summary>
/// Creates isolated artifact sinks for individual model runs. Artifact JSONL is intentionally
/// separate from application diagnostics because concurrent model runs require stable paths.
/// Layout is root/{batch}_{batchStartedAt}/{runId}/{file} - the batch folder is timestamped so
/// repeated invocations of the same batch never collide, and each run within it gets its own
/// run-id subfolder so filenames underneath stay free of the id.
/// </summary>
public sealed class RunArtifactWriter(string? rootOverride = null)
{
    private const string BatchTimestampFormat = "yyyy-MM-dd_HH-mm-ss";

    private string Root =>
        rootOverride
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".manipulator"
        );

    public RunArtifactSession Open(RunConfig config, string runId, DateTimeOffset batchStartedAt)
    {
        var jsonlPath = JsonlPath(config, runId, batchStartedAt);
        var finalScenePath = FinalScenePath(config, runId, batchStartedAt);
        Directory.CreateDirectory(Path.GetDirectoryName(jsonlPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(finalScenePath)!);

        var logger = new LoggerConfiguration()
            .WriteTo.File(new CompactJsonFormatter(), jsonlPath)
            .CreateLogger();

        return new RunArtifactSession(logger, jsonlPath, finalScenePath);
    }

    public string JsonlPath(RunConfig config, string runId, DateTimeOffset batchStartedAt) =>
        Path.Combine(
            RunDir(config, runId, batchStartedAt),
            $"{SanitizeSegment(config.ModelName)}.jsonl"
        );

    public string FinalScenePath(RunConfig config, string runId, DateTimeOffset batchStartedAt) =>
        Path.Combine(
            RunDir(config, runId, batchStartedAt),
            $"{config.Scenario}_{config.Approach}_{SanitizeSegment(config.ModelName)}_{config.RunIndex}.json"
        );

    private string RunDir(RunConfig config, string runId, DateTimeOffset batchStartedAt) =>
        Path.Combine(BatchDir(config, batchStartedAt), SanitizeSegment(runId));

    private string BatchDir(RunConfig config, DateTimeOffset batchStartedAt) =>
        Path.Combine(
            Root,
            $"{SanitizeSegment(config.Batch)}_{batchStartedAt.ToString(BatchTimestampFormat)}"
        );

    private static string SanitizeSegment(string value) =>
        string.Concat(
            value.Select(character =>
                Path.GetInvalidFileNameChars().Contains(character) ? '_' : character
            )
        );
}

public sealed class RunArtifactSession(Logger logger, string jsonlPath, string finalScenePath)
    : IDisposable
{
    public string JsonlPath { get; } = jsonlPath;

    public string FinalScenePath { get; } = finalScenePath;

    public void WriteStep(StepRecord step) => logger.Information("step {@Step}", step);

    public void WriteRun(RunRecord record) => logger.Information("run {@Run}", record);

    public void SaveFinalScene(string sceneJson) => File.WriteAllText(FinalScenePath, sceneJson);

    public void Dispose()
    {
        logger.Dispose();
    }
}
