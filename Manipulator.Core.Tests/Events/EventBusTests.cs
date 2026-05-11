using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;

namespace Manipulator.Core.Tests.Events;

public class EventBusTests
{
    #region TypedSubscriber

    [Fact]
    public void TypedSubscriber_ReceivesOnlyItsType()
    {
        // Arrange
        var eventBus = new EventBus();
        var addedCount = 0;
        var removedCount = 0;

        eventBus.Subscribe<EntityAddedEvent>(_ => addedCount++);
        eventBus.Subscribe<EntityRemovedEvent>(_ => removedCount++);

        var entity = new Entity("entity");
        var addedEvent = new EntityAddedEvent(entity);
        var removedEvent = new EntityRemovedEvent("entity");

        // Act
        eventBus.Publish(addedEvent);
        eventBus.Publish(removedEvent);

        // Assert
        addedCount.Should().Be(1);
        removedCount.Should().Be(1);
    }

    #endregion

    #region CatchAllSubscriber

    [Fact]
    public void CatchAllSubscriber_ReceivesAllEvents()
    {
        // Arrange
        var eventBus = new EventBus();
        var eventCount = 0;

        eventBus.Subscribe<ISceneEvent>(_ => eventCount++);

        var entity = new Entity("entity");

        // Act
        eventBus.Publish(new EntityAddedEvent(entity));
        eventBus.Publish(new EntityRemovedEvent("entity"));
        eventBus.Publish(new ComponentChangedEvent("entity", "Transform", new Transform()));
        eventBus.Publish(new SceneClearedEvent());

        // Assert
        eventCount.Should().Be(4);
    }

    #endregion

    #region ExceptionIsolation

    [Fact]
    public void ExceptionInHandler_DoesNotBlockOtherHandlers()
    {
        // Arrange
        var eventBus = new EventBus();
        var handlerExecuted = false;

        eventBus.Subscribe<EntityAddedEvent>(_ =>
            throw new InvalidOperationException("First handler fails")
        );
        eventBus.Subscribe<EntityAddedEvent>(_ => handlerExecuted = true);

        var entity = new Entity("entity");
        var entityAddedEvent = new EntityAddedEvent(entity);

        // Act
        var action = () => eventBus.Publish(entityAddedEvent);

        // Assert
        action.Should().NotThrow();
        handlerExecuted.Should().BeTrue();
    }

    #endregion

    #region Unsubscribe

    [Fact]
    public void Unsubscribe_HandlerNotCalled()
    {
        // Arrange
        var eventBus = new EventBus();
        var callCount = 0;

        eventBus.Subscribe<EntityAddedEvent>(Handler);
        eventBus.Unsubscribe<EntityAddedEvent>(Handler);

        var entity = new Entity("entity");
        var entityAddedEvent = new EntityAddedEvent(entity);

        // Act
        eventBus.Publish(entityAddedEvent);

        // Assert
        callCount.Should().Be(0);
        return;

        void Handler(EntityAddedEvent _) => callCount++;
    }

    #endregion

    #region DuplicateSubscription

    [Fact]
    public void SameHandlerTwice_CalledTwice()
    {
        // Arrange
        var eventBus = new EventBus();
        var callCount = 0;

        eventBus.Subscribe<EntityAddedEvent>(Handler);
        eventBus.Subscribe<EntityAddedEvent>(Handler);

        var entity = new Entity("entity");
        var entityAddedEvent = new EntityAddedEvent(entity);

        // Act
        eventBus.Publish(entityAddedEvent);

        // Assert
        callCount.Should().Be(2);
        return;

        void Handler(EntityAddedEvent _) => callCount++;
    }

    #endregion

    #region EmptyBus

    [Fact]
    public void NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var eventBus = new EventBus();
        var entity = new Entity("entity");
        var entityAddedEvent = new EntityAddedEvent(entity);

        // Act
        var publishAction = () => eventBus.Publish(entityAddedEvent);

        // Assert
        publishAction.Should().NotThrow();
    }

    #endregion
}
