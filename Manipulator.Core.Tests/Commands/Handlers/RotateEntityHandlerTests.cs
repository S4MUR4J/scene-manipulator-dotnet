using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class RotateEntityHandlerTests
{
    private readonly RotateEntityHandler _handler = new RotateEntityHandler();

    #region Handle — success

    [Fact]
    public void Handle_EntityExists_ReturnsSuccess()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new RotateEntityCommand(SceneBuilder.Id(1), Vector3.Up)
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Handle_EntityExists_UpdatesRotationOnTransform()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new Transform { Rotation = Vector3.Zero }))
            .Build();

        // Act
        _handler.Handle(scene, new RotateEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        scene.GetEntity(SceneBuilder.Id(1))!.Get<Transform>()!.Rotation.Should().Be(Vector3.Up);
    }

    [Fact]
    public void Handle_EntityExists_PreservesPositionAndScale()
    {
        // Arrange
        var original = new Transform
        {
            Position = Vector3.Right,
            Rotation = Vector3.Zero,
            Scale = Vector3.One * 2f,
        };
        var scene = new SceneBuilder().WithEntity(e => e.WithComponent(original)).Build();

        // Act
        _handler.Handle(scene, new RotateEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        var updated = scene.GetEntity(SceneBuilder.Id(1))!.Get<Transform>()!;
        updated.Position.Should().Be(original.Position);
        updated.Scale.Should().Be(original.Scale);
    }

    [Fact]
    public void Handle_EntityExists_ReturnsComponentChangedEvent()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new RotateEntityCommand(SceneBuilder.Id(1), Vector3.Up)
        );

        // Assert
        var ev = result
            .Events.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ComponentChangedEvent>()
            .Subject;
        ev.EntityId.Should().Be(SceneBuilder.Id(1));
        ev.ComponentType.Should().Be(nameof(Transform));
    }

    [Fact]
    public void Handle_EntityExists_BumpsSceneVersion()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();
        var versionBeforeChange = scene.Version;

        // Act
        _handler.Handle(scene, new RotateEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        scene.Version.Should().Be(versionBeforeChange + 1);
    }

    #endregion

    #region Handle — failure

    [Fact]
    public void Handle_MissingEntity_ReturnsFail()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new RotateEntityCommand(SceneBuilder.Id(99), Vector3.Zero)
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(SceneBuilder.Id(99));
    }

    [Theory]
    [InlineData(float.NaN, 0f, 0f)]
    [InlineData(0f, float.PositiveInfinity, 0f)]
    [InlineData(0f, 0f, float.NegativeInfinity)]
    public void Handle_NonFiniteRotation_ReturnsFail(float x, float y, float z)
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new RotateEntityCommand(SceneBuilder.Id(1), new Vector3(x, y, z))
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Handle_Failure_DoesNotBumpSceneVersion()
    {
        // Arrange
        var scene = new SceneBuilder().Build();
        var versionBeforeChange = scene.Version;

        // Act
        _handler.Handle(scene, new RotateEntityCommand(SceneBuilder.Id(99), Vector3.Zero));

        // Assert
        scene.Version.Should().Be(versionBeforeChange);
    }

    #endregion
}
