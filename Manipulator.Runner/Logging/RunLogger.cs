using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;

namespace Manipulator.Runner.Logging;

/// <summary>
/// Path conventions for a batch's output, and the Serilog logger that writes its JSONL (run +
/// per-step records, one JSON object per line via <see cref="CompactJsonFormatter"/>).
/// </summary>
public static class RunLogger
{
    public static string JsonlPath(string outDir, string batch) =>
        Path.Combine(outDir, batch, $"{batch}.jsonl");

    public static string FinalScenePath(string outDir, string batch, RunConfig config) =>
        Path.Combine(outDir, batch, $"{config.Scenario}_{config.Approach}_{config.RunIndex}.json");

    public static Logger CreateLogger(string jsonlPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(jsonlPath)!);
        return new LoggerConfiguration()
            .WriteTo.File(new CompactJsonFormatter(), jsonlPath)
            .CreateLogger();
    }

    public static void SaveFinalScene(string path, string sceneJson)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sceneJson);
    }
}
