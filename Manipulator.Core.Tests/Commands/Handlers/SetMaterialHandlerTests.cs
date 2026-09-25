using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class SetMaterialHandlerTests
{
    private readonly SetMaterialHandler _handler = new SetMaterialHandler();

    #region Handle — success

    [Fact]
    public void Handle_EntityExists_ReturnsSuccess()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new SetMaterialCommand(SceneBuilder.Id(1), Color: "#ff0000")
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Handle_PartialUpdate_OnlyChangesProvidedFields()
    {
        // Arrange
        var original = new MeshRenderer(
            Color: "#ff0000",
            Opacity: 0.5f,
            Metalness: 0.2f,
            Roughness: 0.3f
        );
        var scene = new SceneBuilder().WithEntity(e => e.WithComponent(original)).Build();

        // Act
        _handler.Handle(scene, new SetMaterialCommand(SceneBuilder.Id(1), Opacity: 0.9f));

        // Assert
        var updated = scene.GetEntity(SceneBuilder.Id(1))!.Get<MeshRenderer>()!;
        updated.Color.Should().Be(original.Color);
        updated.Opacity.Should().Be(0.9f);
        updated.Metalness.Should().Be(original.Metalness);
        updated.Roughness.Should().Be(original.Roughness);
    }

    [Fact]
    public void Handle_EntityWithNoMaterial_CreatesMaterialFromDefaults()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        _handler.Handle(scene, new SetMaterialCommand(SceneBuilder.Id(1), Color: "#00ff00"));

        // Assert
        var updated = scene.GetEntity(SceneBuilder.Id(1))!.Get<MeshRenderer>()!;
        updated.Color.Should().Be("#00ff00");
        updated.Opacity.Should().Be(1.0f);
    }

    [Fact]
    public void Handle_EntityExists_ReturnsComponentChangedEvent()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new SetMaterialCommand(SceneBuilder.Id(1), Color: "#ff0000")
        );

        // Assert
        var ev = result
            .Events.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ComponentChangedEvent>()
            .Subject;
        ev.EntityId.Should().Be(SceneBuilder.Id(1));
        ev.ComponentType.Should().Be(nameof(MeshRenderer));
    }

    [Fact]
    public void Handle_EntityExists_BumpsSceneVersion()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();
        var versionBeforeChange = scene.Version;

        // Act
        _handler.Handle(scene, new SetMaterialCommand(SceneBuilder.Id(1), Color: "#ff0000"));

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
            new SetMaterialCommand(SceneBuilder.Id(99), Color: "#ff0000")
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(SceneBuilder.Id(99));
    }

    [Fact]
    public void Handle_InvalidHexColor_ReturnsFail()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new SetMaterialCommand(SceneBuilder.Id(1), Color: "red")
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Handle_OpacityOutOfRange_ReturnsFail(float opacity)
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = _handler.Handle(
            scene,
            new SetMaterialCommand(SceneBuilder.Id(1), Opacity: opacity)
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
        _handler.Handle(scene, new SetMaterialCommand(SceneBuilder.Id(1), Color: "red"));

        // Assert
        scene.Version.Should().Be(versionBeforeChange);
    }

    #endregion
}
