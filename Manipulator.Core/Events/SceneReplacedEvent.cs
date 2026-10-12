namespace Manipulator.Core.Events;

public record SceneReplacedEvent(IReadOnlyList<string> EntityIds) : ISceneEvent;
