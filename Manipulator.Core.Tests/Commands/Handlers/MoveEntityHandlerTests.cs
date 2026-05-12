using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class MoveEntityHandlerTests
{
    private readonly MoveEntityHandler _handler = new();

    #region Handle — success

    [Fact]
    public void Handle_EntityExists_ReturnsSuccess()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.One));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Handle_EntityExists_UpdatesPositionOnTransform()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new Transform { Position = Vector3.Zero }))
            .Build();

        // Act
        _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        scene.GetEntity(SceneBuilder.Id(1))!.Get<Transform>()!.Position.Should().Be(Vector3.Up);
    }

    [Fact]
    public void Handle_EntityExists_PreservesRotationAndScale()
    {
        // Arrange
        var original = new Transform { Position = Vector3.Zero, Rotation = Vector3.Right, Scale = Vector3.One * 2f };
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(original))
            .Build();

        // Act
        _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        var updated = scene.GetEntity(SceneBuilder.Id(1))!.Get<Transform>()!;
        updated.Rotation.Should().Be(original.Rotation);
        updated.Scale.Should().Be(original.Scale);
    }

    [Fact]
    public void Handle_EntityExists_ReturnsComponentChangedEvent()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.One));

        // Assert
        result.Events.Should().ContainSingle().Which.Should().BeOfType<ComponentChangedEvent>();
    }

    [Fact]
    public void Handle_EntityExists_EventHasCorrectEntityId()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.One));

        // Assert
        var ev = result.Events.Single().Should().BeOfType<ComponentChangedEvent>().Subject;
        ev.EntityId.Should().Be(SceneBuilder.Id(1));
    }

    [Fact]
    public void Handle_EntityExists_EventHasCorrectComponentType()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.One));

        // Assert
        var ev = result.Events.Single().Should().BeOfType<ComponentChangedEvent>().Subject;
        ev.ComponentType.Should().Be(nameof(Transform));
    }

    [Fact]
    public void Handle_EntityExists_EventNewValueHasUpdatedPosition()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new Transform { Position = Vector3.Zero }))
            .Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Forward));

        // Assert
        var ev = result.Events.Single().Should().BeOfType<ComponentChangedEvent>().Subject;
        ev.NewValue.Should().BeOfType<Transform>().Which.Position.Should().Be(Vector3.Forward);
    }

    [Fact]
    public void Handle_EntityExists_EventOldValueHasOriginalPosition()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new Transform { Position = Vector3.Right }))
            .Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        var ev = result.Events.Single().Should().BeOfType<ComponentChangedEvent>().Subject;
        ev.OldValue.Should().BeOfType<Transform>().Which.Position.Should().Be(Vector3.Right);
    }

    #endregion

    #region Handle — no existing Transform

    [Fact]
    public void Handle_EntityWithNoTransform_ReturnsSuccess()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Handle_EntityWithNoTransform_SetsPosition()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        scene.GetEntity(SceneBuilder.Id(1))!.Get<Transform>()!.Position.Should().Be(Vector3.Up);
    }

    #endregion

    #region Handle — failure

    [Fact]
    public void Handle_MissingEntity_ReturnsFail()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(99), Vector3.Zero));

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Handle_MissingEntity_ErrorMentionsEntityId()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var result = _handler.Handle(scene, new MoveEntityCommand(SceneBuilder.Id(99), Vector3.Zero));

        // Assert
        result.Error.Should().Contain(SceneBuilder.Id(99));
    }

    #endregion
}
