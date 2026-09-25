using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;

namespace Manipulator.Mcp.Session;

/// <summary>
/// The optional scene every session starts from. The file is read and parsed once at startup so a
/// broken scenario fails before any agent connects, instead of silently running against an empty
/// scene and producing a run that looks valid but measures nothing.
/// </summary>
public sealed class StartingScene
{
    private readonly string? _json;

    public StartingScene(RunOptions options)
    {
        Path = options.StartingScenePath;
        if (Path is null)
        {
            Warnings = [];
            return;
        }

        if (!File.Exists(Path))
            throw new InvalidOperationException($"Starting scene file '{Path}' does not exist.");

        var json = File.ReadAllText(Path);
        try
        {
            Warnings = SceneSerializer.Deserialize(json).Warnings;
        }
        catch (SceneDeserializationException ex)
        {
            throw new InvalidOperationException(
                $"Starting scene file '{Path}' is not a valid scene: {ex.Message}",
                ex
            );
        }

        _json = json;
    }

    public string? Path { get; }

    public IReadOnlyList<string> Warnings { get; } = [];

    /// <summary>
    /// Builds a fresh scene per session — deserializing again rather than sharing one instance,
    /// so two sessions on the same server can never see each other's edits.
    /// </summary>
    public Scene Create()
    {
        return _json is null ? new Scene() : SceneSerializer.Deserialize(_json).Scene;
    }
}
