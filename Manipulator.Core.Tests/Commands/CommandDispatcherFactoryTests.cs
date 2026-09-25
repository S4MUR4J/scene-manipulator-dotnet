using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands;

public class CommandDispatcherFactoryTests
{
    private readonly Scene _scene = new SceneBuilder().Build();
    private readonly EventBus _eventBus = new EventBus();
    private readonly CommandDispatcher _dispatcher;

    public CommandDispatcherFactoryTests()
    {
        _dispatcher = CommandDispatcherFactory.Create(_scene, _eventBus, new TestGuidGenerator());
    }

    #region Create — registers every command

    [Fact]
    public void Create_AddEntity_IsDispatchable()
    {
        // Act
        var result = _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_MoveEntity_IsDispatchable()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Act
        var result = _dispatcher.Dispatch(new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_RotateEntity_IsDispatchable()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Act
        var result = _dispatcher.Dispatch(new RotateEntityCommand(SceneBuilder.Id(1), Vector3.Up));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ScaleEntity_IsDispatchable()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Act
        var result = _dispatcher.Dispatch(
            new ScaleEntityCommand(SceneBuilder.Id(1), Vector3.One * 2f)
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_SetMaterial_IsDispatchable()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Act
        var result = _dispatcher.Dispatch(
            new SetMaterialCommand(SceneBuilder.Id(1), Color: "#ff0000")
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_RenameEntity_IsDispatchable()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Act
        var result = _dispatcher.Dispatch(new RenameEntityCommand(SceneBuilder.Id(1), "Cube"));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_RemoveEntity_IsDispatchable()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Act
        var result = _dispatcher.Dispatch(new RemoveEntityCommand(SceneBuilder.Id(1)));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Create — wires EntityExistsValidator

    [Fact]
    public void Create_MoveEntity_MissingEntity_ReturnsFail()
    {
        // Act
        var result = _dispatcher.Dispatch(new MoveEntityCommand(SceneBuilder.Id(99), Vector3.Up));

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Create — wires VersionConflictValidator

    [Fact]
    public void Create_MoveEntity_WrongExpectedVersion_ReturnsFail()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube));

        // Act
        var result = _dispatcher.Dispatch(
            new MoveEntityCommand(SceneBuilder.Id(1), Vector3.Up, ExpectedVersion: 999)
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion
}
