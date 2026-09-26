using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;

namespace Manipulator.Runner;

internal static class StartingScene
{
    public static Scene Load(string? path)
    {
        if (path is null)
            return new Scene();

        if (!File.Exists(path))
            throw new InvalidOperationException($"Starting scene file '{path}' does not exist.");

        var json = File.ReadAllText(path);
        try
        {
            return SceneSerializer.Deserialize(json).Scene;
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
