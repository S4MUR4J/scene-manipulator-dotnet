using Manipulator.Core.Ecs;

namespace Manipulator.Core.Serialization;

public sealed record SceneDeserializationResult(Scene Scene, IReadOnlyList<string> Warnings);
