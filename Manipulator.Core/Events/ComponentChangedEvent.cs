using Manipulator.Core.Ecs;

namespace Manipulator.Core.Events;

public record ComponentChangedEvent(
    string EntityId,
    string ComponentType,
    IComponent NewValue,
    IComponent? OldValue = null
) : ISceneEvent;
