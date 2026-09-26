using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp.Tests;

public class SceneWriteToolsTests
{
    private readonly Scene _scene = new Scene();
    private readonly CommandDispatcher _dispatcher;
    private readonly SceneWriteTools _sut;

    public SceneWriteToolsTests()
    {
        _dispatcher = CommandDispatcherFactory.Create(_scene, new EventBus());
        _sut = new SceneWriteTools(_scene, _dispatcher);
    }

    private string AddEntity()
    {
        var result = _dispatcher.Dispatch(new AddEntityCommand(Geometry: GeometryType.Cube));
        return (string)result.Data!;
    }

    #region add_entity

    [Fact]
    public void AddEntity_ValidGeometry_ReturnsOkWithEntityId()
    {
        // Act
        var json = _sut.AddEntity(geometry: "Cube");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["entity_id"]!.GetValue<string>().Should().NotBeNullOrEmpty();
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(1);
    }

    [Fact]
    public void AddEntity_UnknownGeometry_ReturnsBadRequest()
    {
        // Act
        var json = _sut.AddEntity(geometry: "Blob");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("Unknown geometry 'Blob'");
    }

    [Fact]
    public void AddEntity_PositionWrongLength_ReturnsBadRequest()
    {
        // Act
        var json = _sut.AddEntity(geometry: "Cube", position: [1, 2]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("exactly 3 numbers");
    }

    [Fact]
    public void AddEntity_ScaleIsNotPositive_ReturnsBadRequest()
    {
        // Act
        var json = _sut.AddEntity(geometry: "Cube", scale: [0, 1, 1]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("Scale must be positive");
    }

    #endregion

    #region move_entity

    [Fact]
    public void MoveEntity_EntityExists_ReturnsOkWithUpdatedSceneVersion()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.MoveEntity(id, [1, 2, 3]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["entity_id"]!.GetValue<string>().Should().Be(id);
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(2);
    }

    [Fact]
    public void MoveEntity_EntityDoesNotExist_ReturnsBadRequest()
    {
        // Act
        var json = _sut.MoveEntity("missing", [1, 2, 3]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("does not exist");
    }

    [Fact]
    public void MoveEntity_PositionWrongLength_ReturnsBadRequest()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.MoveEntity(id, [1, 2]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("exactly 3 numbers");
    }

    #endregion

    #region rotate_entity

    [Fact]
    public void RotateEntity_EntityExists_ReturnsOkWithUpdatedSceneVersion()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.RotateEntity(id, [0, 90, 0]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(2);
    }

    [Fact]
    public void RotateEntity_EntityDoesNotExist_ReturnsBadRequest()
    {
        // Act
        var json = _sut.RotateEntity("missing", [0, 90, 0]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("does not exist");
    }

    #endregion

    #region scale_entity

    [Fact]
    public void ScaleEntity_EntityExists_ReturnsOkWithUpdatedSceneVersion()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.ScaleEntity(id, [2, 2, 2]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(2);
    }

    [Fact]
    public void ScaleEntity_ScaleIsNotPositive_ReturnsBadRequest()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.ScaleEntity(id, [0, 1, 1]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("Scale must be positive");
    }

    [Fact]
    public void ScaleEntity_EntityDoesNotExist_ReturnsBadRequest()
    {
        // Act
        var json = _sut.ScaleEntity("missing", [1, 1, 1]);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("does not exist");
    }

    #endregion

    #region set_material

    [Fact]
    public void SetMaterial_EntityExists_ReturnsOkWithUpdatedSceneVersion()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.SetMaterial(id, color: "#ff0000");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(2);
    }

    [Fact]
    public void SetMaterial_OpacityOutOfRange_ReturnsBadRequest()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.SetMaterial(id, opacity: 2f);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
    }

    [Fact]
    public void SetMaterial_EntityDoesNotExist_ReturnsBadRequest()
    {
        // Act
        var json = _sut.SetMaterial("missing", color: "#ff0000");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("does not exist");
    }

    #endregion

    #region rename_entity

    [Fact]
    public void RenameEntity_EntityExists_ReturnsOkWithUpdatedSceneVersion()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.RenameEntity(id, "table top");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(2);
    }

    [Fact]
    public void RenameEntity_EntityDoesNotExist_ReturnsBadRequest()
    {
        // Act
        var json = _sut.RenameEntity("missing", "table top");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("does not exist");
    }

    #endregion

    #region remove_entity

    [Fact]
    public void RemoveEntity_EntityExists_ReturnsOkWithUpdatedSceneVersion()
    {
        // Arrange
        var id = AddEntity();

        // Act
        var json = _sut.RemoveEntity(id);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(2);
        _scene.HasEntity(id).Should().BeFalse();
    }

    [Fact]
    public void RemoveEntity_EntityDoesNotExist_ReturnsBadRequest()
    {
        // Act
        var json = _sut.RemoveEntity("missing");

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("does not exist");
    }

    #endregion
}
