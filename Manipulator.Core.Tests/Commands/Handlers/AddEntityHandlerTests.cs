using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class AddEntityHandlerTests
{
    private readonly Scene _scene = new SceneBuilder().Build();
    private readonly TestGuidGenerator _idGen = new TestGuidGenerator();
    private readonly AddEntityHandler _handler;

    public AddEntityHandlerTests()
    {
        _handler = new AddEntityHandler(_idGen);
    }

    #region Handle

    [Fact]
    public void Handle_CreatesEntityWithAllFourComponents()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Has<Transform>().Should().BeTrue();
        entity.Has<MeshFilter>().Should().BeTrue();
        entity.Has<MeshRenderer>().Should().BeTrue();
        entity.Has<EntityName>().Should().BeTrue();
    }

    [Fact]
    public void Handle_ReturnsEntityIdInData()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Sphere));

        // Assert
        result.Data.Should().Be("entity_1");
    }

    [Fact]
    public void Handle_ReturnsEntityAddedEvent()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        result.Events.Should().ContainSingle().Which.Should().BeOfType<EntityAddedEvent>();
    }

    [Fact]
    public void Handle_UsesProvidedPosition()
    {
        // Arrange
        var pos = new Vector3(1, 2, 3);

        // Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Position: pos)
        );

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<Transform>()!.Position.Should().Be(pos);
    }

    [Fact]
    public void Handle_DefaultPosition_IsZero()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<Transform>()!.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Handle_UsesProvidedName()
    {
        // Arrange & Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Name: "MyCube")
        );

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<EntityName>()!.Value.Should().Be("MyCube");
    }

    [Fact]
    public void Handle_DefaultName_IsEmpty()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<EntityName>()!.Value.Should().BeEmpty();
    }

    [Fact]
    public void Handle_UsesProvidedColor()
    {
        // Arrange & Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Color: "#ff0000")
        );

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<MeshRenderer>()!.Color.Should().Be("#ff0000");
    }

    [Fact]
    public void Handle_DefaultColor_IsWhite()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<MeshRenderer>()!.Color.Should().Be("#ffffff");
    }

    [Fact]
    public void Handle_UsesProvidedRotationAndScale()
    {
        // Arrange
        var rotation = new Vector3(0, 90, 0);
        var scale = new Vector3(2, 2, 2);

        // Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Rotation: rotation, Scale: scale)
        );

        // Assert
        var transform = _scene.GetEntity((string)result.Data!)!.Get<Transform>()!;
        transform.Rotation.Should().Be(rotation);
        transform.Scale.Should().Be(scale);
    }

    [Fact]
    public void Handle_DefaultRotation_IsZero()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<Transform>()!.Rotation.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Handle_DefaultScale_IsOne()
    {
        // Arrange & Act
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        var entity = _scene.GetEntity((string)result.Data!)!;
        entity.Get<Transform>()!.Scale.Should().Be(Vector3.One);
    }

    [Fact]
    public void Handle_UsesProvidedMaterialFields()
    {
        // Arrange & Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Opacity: 0.5f, Metalness: 0.8f, Roughness: 0.2f)
        );

        // Assert
        var material = _scene.GetEntity((string)result.Data!)!.Get<MeshRenderer>()!;
        material.Opacity.Should().Be(0.5f);
        material.Metalness.Should().Be(0.8f);
        material.Roughness.Should().Be(0.2f);
    }

    #endregion

    #region Handle — validation failures

    [Fact]
    public void Handle_NonFinitePosition_ReturnsFail()
    {
        // Arrange & Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Position: new Vector3(float.NaN, 0, 0))
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(0f, 1f, 1f)]
    [InlineData(-1f, 1f, 1f)]
    public void Handle_NonPositiveScale_ReturnsFail(float x, float y, float z)
    {
        // Arrange & Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Scale: new Vector3(x, y, z))
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Handle_InvalidHexColor_ReturnsFail()
    {
        // Arrange & Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Color: "not-a-color")
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Handle_OpacityOutOfRange_ReturnsFail()
    {
        // Arrange & Act
        var result = _handler.Handle(
            _scene,
            new AddEntityCommand(GeometryType.Cube, Opacity: 1.5f)
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Handle_Failure_DoesNotAddEntityToScene()
    {
        // Arrange & Act
        _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube, Color: "not-a-color"));

        // Assert
        _scene.Count.Should().Be(0);
    }

    [Fact]
    public void Handle_Failure_DoesNotBumpSceneVersion()
    {
        // Arrange
        var versionBeforeChange = _scene.Version;

        // Act
        _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube, Color: "not-a-color"));

        // Assert
        _scene.Version.Should().Be(versionBeforeChange);
    }

    #endregion

    #region Handle — version bump

    [Fact]
    public void Handle_Success_BumpsSceneVersionOnce()
    {
        // Arrange
        var versionBeforeChange = _scene.Version;

        // Act
        _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube));

        // Assert
        _scene.Version.Should().Be(versionBeforeChange + 1);
    }

    #endregion
}
