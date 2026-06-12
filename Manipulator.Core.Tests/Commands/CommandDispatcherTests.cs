using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands;

public class CommandDispatcherTests
{
    private readonly Scene _scene = new SceneBuilder().Build();
    private readonly EventBus _eventBus = new EventBus();
    private readonly CommandDispatcher _dispatcher;

    public CommandDispatcherTests()
    {
        _dispatcher = new CommandDispatcher(_scene, _eventBus);
    }

    #region Register

    [Fact]
    public void Register_SameType_OverwritesPreviousHandler()
    {
        // Arrange
        _dispatcher.Register("Fake", new FailHandler());
        _dispatcher.Register("Fake", new SuccessHandler());

        // Act
        var result = _dispatcher.Dispatch(new FakeCommand());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Dispatch

    [Fact]
    public void Dispatch_NoHandlerRegistered_ReturnsFail()
    {
        // Arrange
        var command = new FakeCommand();

        // Act
        var result = _dispatcher.Dispatch(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Dispatch_HandlerThrows_ReturnsFail()
    {
        // Arrange
        _dispatcher.Register("Fake", new ThrowingHandler());

        // Act
        var result = _dispatcher.Dispatch(new FakeCommand());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Dispatch_HandlerThrows_SceneUnchanged()
    {
        // Arrange
        _dispatcher.Register("Fake", new ThrowingHandler());

        // Act
        _dispatcher.Dispatch(new FakeCommand());

        // Assert
        _scene.Count.Should().Be(0);
    }

    [Fact]
    public void Dispatch_Success_ReturnsSuccess()
    {
        // Arrange
        _dispatcher.Register("Fake", new SuccessHandler());

        // Act
        var result = _dispatcher.Dispatch(new FakeCommand());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Dispatch_Success_PublishesEventsToEventBus()
    {
        // Arrange
        _dispatcher.Register("Fake", new SuccessHandler());
        ISceneEvent? received = null;
        _eventBus.Subscribe<FakeEvent>(e => received = e);

        // Act
        _dispatcher.Dispatch(new FakeCommand());

        // Assert
        received.Should().NotBeNull();
    }

    [Fact]
    public void Dispatch_Fail_DoesNotPublishEvents()
    {
        // Arrange
        _dispatcher.Register("Fake", new FailHandler());
        var eventPublished = false;
        _eventBus.Subscribe<FakeEvent>(_ => eventPublished = true);

        // Act
        _dispatcher.Dispatch(new FakeCommand());

        // Assert
        eventPublished.Should().BeFalse();
    }

    #endregion
}
