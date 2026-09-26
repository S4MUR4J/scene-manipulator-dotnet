using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp.Tests;

public class SceneReadToolsTests
{
    private readonly Scene _scene = new Scene();
    private readonly CommandDispatcher _dispatcher;
    private readonly SceneReadTools _sut;

    public SceneReadToolsTests()
    {
        _dispatcher = CommandDispatcherFactory.Create(_scene, new EventBus());
        _sut = new SceneReadTools(_scene);
    }

    private string AddEntity()
    {
        var result = _dispatcher.Dispatch(new AddEntityCommand(Geometry: GeometryType.Cube));
        return (string)result.Data!;
    }

    #region get_scene

    [Fact]
    public void GetScene_SceneIsEmpty_ReturnsOkWithNoEntities()
    {
        // Act
        var json = _sut.GetScene();

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["scene"]!["entities"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public void GetScene_SceneHasEntities_ReturnsOkWithSerializedEntity()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.GetScene();

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        var entities = node["result"]!["scene"]!["entities"]!.AsArray();
        entities.Should().ContainSingle(entity => entity!["id"]!.GetValue<string>() == id);
    }

    #endregion

    #region get_entity

    [Fact]
    public void GetEntity_EntityExists_ReturnsOkWithSerializedEntity()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.GetEntity(id);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["entity"]!["id"]!.GetValue<string>().Should().Be(id);
    }

    [Fact]
    public void GetEntity_EntityDoesNotExist_ReturnsNotFound()
    {
        // Act
        var json = _sut.GetEntity("missing");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(404);
        node["error"]!.GetValue<string>().Should().Contain("does not exist");
    }

    [Fact]
    public void GetEntity_EntityIdIsEmpty_ReturnsBadRequest()
    {
        // Act
        var json = _sut.GetEntity("   ");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("is required");
    }

    #endregion
}
