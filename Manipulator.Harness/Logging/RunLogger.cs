using System.Text.Encodings.Web;
using System.Text.Json;

namespace Manipulator.Harness.Logging;

/// <summary>
/// Appends JSONL records (run + optional per-step) for a batch, and saves the final scene as a
/// standalone plain-scene-format JSON file for the existing renderers.
/// </summary>
public static class RunLogger
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string JsonlPath(string outDir, string batch) =>
        Path.Combine(outDir, batch, $"{batch}.jsonl");

    public static string FinalScenePath(string outDir, string batch, RunConfig config) =>
        Path.Combine(outDir, batch, $"{config.Scenario}_{config.Approach}_{config.RunIndex}.json");

    public static void AppendRecord<T>(string path, T record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream);
        writer.WriteLine(JsonSerializer.Serialize(record, Options));
    }

    public static void SaveFinalScene(string path, string sceneJson)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sceneJson);
    }
}
