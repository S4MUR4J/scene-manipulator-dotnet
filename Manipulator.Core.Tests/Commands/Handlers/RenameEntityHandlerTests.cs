using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class RenameEntityHandlerTests
{
    private readonly RenameEntityHandler _handler = new RenameEntityHandler();

    #region Handle — success

    [Fact]
    public void Handle_EntityExists_ReturnsSuccess()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(scene, new RenameEntityCommand(SceneBuilder.Id(1), "Cube"));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Handle_EntityExists_UpdatesEntityName()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new EntityName("Old")))
            .Build();

        // Act
        _handler.Handle(scene, new RenameEntityCommand(SceneBuilder.Id(1), "New"));

        // Assert
        scene.GetEntity(SceneBuilder.Id(1))!.Get<EntityName>()!.Value.Should().Be("New");
    }

    [Fact]
    public void Handle_EntityWithNoName_SetsName()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        _handler.Handle(scene, new RenameEntityCommand(SceneBuilder.Id(1), "Cube"));

        // Assert
        scene.GetEntity(SceneBuilder.Id(1))!.Get<EntityName>()!.Value.Should().Be("Cube");
    }

    [Fact]
    public void Handle_EntityExists_ReturnsComponentChangedEvent()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(scene, new RenameEntityCommand(SceneBuilder.Id(1), "Cube"));

        // Assert
        var ev = result
            .Events.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ComponentChangedEvent>()
            .Subject;
        ev.EntityId.Should().Be(SceneBuilder.Id(1));
        ev.ComponentType.Should().Be(nameof(EntityName));
    }

    [Fact]
    public void Handle_EntityExists_BumpsSceneVersion()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();
        var versionBeforeChange = scene.Version;

        // Act
        _handler.Handle(scene, new RenameEntityCommand(SceneBuilder.Id(1), "Cube"));

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
        var result = _handler.Handle(scene, new RenameEntityCommand(SceneBuilder.Id(99), "Cube"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(SceneBuilder.Id(99));
    }

    [Fact]
    public void Handle_Failure_DoesNotBumpSceneVersion()
    {
        // Arrange
        var scene = new SceneBuilder().Build();
        var versionBeforeChange = scene.Version;

        // Act
        _handler.Handle(scene, new RenameEntityCommand(SceneBuilder.Id(99), "Cube"));

        // Assert
        scene.Version.Should().Be(versionBeforeChange);
    }

    #endregion
}
