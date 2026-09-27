using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;
using Manipulator.Runner.Configuration;

namespace Manipulator.Runner.Logging;

/// <summary>
/// Creates isolated artifact sinks for individual model runs. Artifact JSONL is intentionally
/// separate from application diagnostics because concurrent model runs require stable paths.
/// </summary>
public sealed class RunArtifactWriter
{
    public RunArtifactSession Open(RunConfig config)
    {
        var jsonlPath = JsonlPath(config.OutDir, config.Batch, config);
        var finalScenePath = FinalScenePath(config.OutDir, config.Batch, config);
        Directory.CreateDirectory(Path.GetDirectoryName(jsonlPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(finalScenePath)!);

        var logger = new LoggerConfiguration()
            .WriteTo.File(new CompactJsonFormatter(), jsonlPath)
            .CreateLogger();

        return new RunArtifactSession(logger, jsonlPath, finalScenePath);
    }

    public string JsonlPath(string outDir, string batch, RunConfig config) =>
        Path.Combine(outDir, batch, $"{SanitizeSegment(config.ModelName)}.jsonl");

    public string FinalScenePath(string outDir, string batch, RunConfig config) =>
        Path.Combine(
            outDir,
            batch,
            $"{config.Scenario}_{config.Approach}_{SanitizeSegment(config.ModelName)}_{config.RunIndex}.json"
        );

    private static string SanitizeSegment(string value) =>
        string.Concat(
            value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character)
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
