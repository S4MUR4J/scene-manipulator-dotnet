using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

/// <summary>
/// Replaces every entity in the scene with the entities of <paramref name="Scene"/>, keeping
/// their ids as given. The handler takes ownership of those entities, so the source scene must
/// not be used afterwards.
/// </summary>
public record ReplaceSceneCommand(Scene Scene, long? ExpectedVersion = null) : ICommand
{
    public string Type => "ReplaceScene";
}
