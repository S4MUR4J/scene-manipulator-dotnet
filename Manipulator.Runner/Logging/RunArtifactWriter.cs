using Manipulator.Runner.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;

namespace Manipulator.Runner.Logging;

/// <summary>
/// Creates isolated artifact sinks for individual model runs. Artifact JSONL is intentionally
/// separate from application diagnostics because concurrent model runs require stable paths.
/// The run id is folded into every artifact path so repeated runs never collide by default.
/// </summary>
public sealed class RunArtifactWriter(string? rootOverride = null)
{
    private string Root =>
        rootOverride
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".manipulator"
        );

    public RunArtifactSession Open(RunConfig config, string runId)
    {
        var jsonlPath = JsonlPath(config, runId);
        var finalScenePath = FinalScenePath(config, runId);
        Directory.CreateDirectory(Path.GetDirectoryName(jsonlPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(finalScenePath)!);

        var logger = new LoggerConfiguration()
            .WriteTo.File(new CompactJsonFormatter(), jsonlPath)
            .CreateLogger();

        return new RunArtifactSession(logger, jsonlPath, finalScenePath);
    }

    public string JsonlPath(RunConfig config, string runId) =>
        Path.Combine(Root, config.Batch, $"{SanitizeSegment(config.ModelName)}_{runId}.jsonl");

    public string FinalScenePath(RunConfig config, string runId) =>
        Path.Combine(
            Root,
            config.Batch,
            $"{config.Scenario}_{config.Approach}_{SanitizeSegment(config.ModelName)}_{config.RunIndex}_{runId}.json"
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
