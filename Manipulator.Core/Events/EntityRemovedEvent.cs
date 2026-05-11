namespace Manipulator.Core.Events;

public record EntityRemovedEvent(string EntityId) : ISceneEvent;
