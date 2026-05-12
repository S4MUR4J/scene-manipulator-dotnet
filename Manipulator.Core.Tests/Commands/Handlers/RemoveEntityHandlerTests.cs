using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class RemoveEntityHandlerTests
{
    private readonly Scene _scene =
        new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1))
            .Build();
    private readonly RemoveEntityHandler _handler = new();

    [Fact]
    public void Handle_ExistingEntity_ReturnsSuccess()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new RemoveEntityCommand(SceneBuilder.Id(1)));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Handle_ExistingEntity_RemovesFromScene()
    {
        // Arrange & Act
        _handler.Handle(_scene, new RemoveEntityCommand(SceneBuilder.Id(1)));

        // Assert
        _scene.HasEntity(SceneBuilder.Id(1)).Should().BeFalse();
    }

    [Fact]
    public void Handle_ExistingEntity_ReturnsEntityRemovedEvent()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new RemoveEntityCommand(SceneBuilder.Id(1)));

        // Assert
        result.Events.Should().ContainSingle().Which.Should().BeOfType<EntityRemovedEvent>();
    }

    [Fact]
    public void Handle_MissingEntity_ReturnsFail()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new RemoveEntityCommand("nonexistent"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}