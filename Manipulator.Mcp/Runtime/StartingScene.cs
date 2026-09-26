using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;

namespace Manipulator.Mcp.Runtime;

/// <summary>
/// The optional scene the run starts from, read and parsed while the process starts up so a broken
/// scenario fails before any agent connects instead of silently running against an empty scene and
/// producing a run that looks valid but measures nothing.
/// </summary>
public sealed record StartingScene(string? Path, Scene Scene, IReadOnlyList<string> Warnings)
{
    public static StartingScene Load(RunOptions options)
    {
        var path = options.StartingScenePath;
        if (path is null)
            return new StartingScene(null, new Scene(), []);

        if (!File.Exists(path))
            throw new InvalidOperationException($"Starting scene file '{path}' does not exist.");

        try
        {
            var result = SceneSerializer.Deserialize(File.ReadAllText(path));
            return new StartingScene(path, result.Scene, result.Warnings);
        }
        catch (SceneDeserializationException ex)
        {
            throw new InvalidOperationException(
                $"Starting scene file '{path}' is not a valid scene: {ex.Message}",
                ex
            );
        }
    }
}
