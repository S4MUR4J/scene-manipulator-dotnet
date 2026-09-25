using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class ScaleEntityHandlerTests
{
    private readonly ScaleEntityHandler _handler = new ScaleEntityHandler();

    #region Handle — success

    [Fact]
    public void Handle_EntityExists_ReturnsSuccess()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new ScaleEntityCommand(SceneBuilder.Id(1), Vector3.One * 2f)
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Handle_EntityExists_UpdatesScaleOnTransform()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new Transform { Scale = Vector3.One }))
            .Build();

        // Act
        _handler.Handle(scene, new ScaleEntityCommand(SceneBuilder.Id(1), Vector3.One * 3f));

        // Assert
        scene.GetEntity(SceneBuilder.Id(1))!.Get<Transform>()!.Scale.Should().Be(Vector3.One * 3f);
    }

    [Fact]
    public void Handle_EntityExists_PreservesPositionAndRotation()
    {
        // Arrange
        var original = new Transform
        {
            Position = Vector3.Right,
            Rotation = Vector3.Up,
            Scale = Vector3.One,
        };
        var scene = new SceneBuilder().WithEntity(e => e.WithComponent(original)).Build();

        // Act
        _handler.Handle(scene, new ScaleEntityCommand(SceneBuilder.Id(1), Vector3.One * 2f));

        // Assert
        var updated = scene.GetEntity(SceneBuilder.Id(1))!.Get<Transform>()!;
        updated.Position.Should().Be(original.Position);
        updated.Rotation.Should().Be(original.Rotation);
    }

    [Fact]
    public void Handle_EntityExists_ReturnsComponentChangedEvent()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new ScaleEntityCommand(SceneBuilder.Id(1), Vector3.One * 2f)
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
        _handler.Handle(scene, new ScaleEntityCommand(SceneBuilder.Id(1), Vector3.One * 2f));

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
            new ScaleEntityCommand(SceneBuilder.Id(99), Vector3.One)
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(SceneBuilder.Id(99));
    }

    [Theory]
    [InlineData(0f, 1f, 1f)]
    [InlineData(1f, 0f, 1f)]
    [InlineData(1f, 1f, 0f)]
    [InlineData(-1f, 1f, 1f)]
    public void Handle_NonPositiveScale_ReturnsFail(float x, float y, float z)
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new ScaleEntityCommand(SceneBuilder.Id(1), new Vector3(x, y, z))
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(float.NaN, 1f, 1f)]
    [InlineData(1f, float.PositiveInfinity, 1f)]
    public void Handle_NonFiniteScale_ReturnsFail(float x, float y, float z)
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new ScaleEntityCommand(SceneBuilder.Id(1), new Vector3(x, y, z))
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Handle_Failure_DoesNotBumpSceneVersion()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();
        var versionBeforeChange = scene.Version;

        // Act
        _handler.Handle(scene, new ScaleEntityCommand(SceneBuilder.Id(1), Vector3.Zero));

        // Assert
        scene.Version.Should().Be(versionBeforeChange);
    }

    #endregion
}
