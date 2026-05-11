using Manipulator.Core.Ecs;

namespace Manipulator.Core.Events;

public record EntityAddedEvent(Entity Entity) : ISceneEvent;
