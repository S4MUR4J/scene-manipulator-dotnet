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
    private readonly TestGuidGenerator _idGen = new();
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
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube, Position: pos));

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
        var result = _handler.Handle(_scene, new AddEntityCommand(GeometryType.Cube, Name: "MyCube"));

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

    #endregion
}