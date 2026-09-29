using System.Text.Json.Nodes;
using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp.Tests;

public class SceneTextToolsTests
{
    private readonly Scene _scene = new Scene();
    private readonly CommandDispatcher _dispatcher;
    private readonly SceneTextTools _sut;

    public SceneTextToolsTests()
    {
        _dispatcher = CommandDispatcherFactory.Create(_scene, new EventBus());
        _sut = new SceneTextTools(_scene, _dispatcher);
    }

    private static JsonObject Scene(string json) => JsonNode.Parse(json)!.AsObject();

    private const string TwoEntities = """
        {"entities": [
          {"id": "sofa", "components": {
            "transform": {"position": [0, 0.5, 0]},
            "mesh_filter": {"geometry": "Cube"},
            "mesh_renderer": {"color": "#336699"},
            "entity_name": {"value": "sofa"}}},
          {"id": "lamp", "components": {
            "transform": {"position": [2, 1, 0]},
            "mesh_filter": {"geometry": "Cylinder"},
            "mesh_renderer": {},
            "entity_name": {"value": "floor lamp"}}}
        ]}
        """;

    #region get_scene

    [Fact]
    public void GetScene_MatchesSceneReadTools()
    {
        // Arrange
        _dispatcher.Dispatch(new AddEntityCommand(Geometry: GeometryType.Cube));

        // Act
        var json = _sut.GetScene();

        // Assert
        json.Should().Be(new SceneReadTools(_scene).GetScene());
    }

    [Fact]
    public void GetScene_AfterSubmit_RoundTripsSubmittedEntities()
    {
        // Arrange
        _sut.SubmitScene(Scene(TwoEntities));

        // Act
        var node = ToolResponse.Parse(_sut.GetScene());

        // Assert
        node["result"]!["scene"]!["entities"]!
            .AsArray()
            .Select(e => e!["id"]!.GetValue<string>())
            .Should()
            .BeEquivalentTo("sofa", "lamp");
    }

    #endregion

    #region submit_scene

    [Fact]
    public void SubmitScene_ValidScene_ReturnsOkWithVersionCountAndNoWarnings()
    {
        // Act
        var node = ToolResponse.Parse(_sut.SubmitScene(Scene(TwoEntities)));

        // Assert
        node["code"]!.GetValue<int>().Should().Be(200);
        node["result"]!["scene_version"]!.GetValue<long>().Should().Be(_scene.Version);
        node["result"]!["entity_count"]!.GetValue<int>().Should().Be(2);
        node["result"]!["warnings"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public void SubmitScene_ReplacesExistingEntities()
    {
        // Arrange
        var oldId = (string)_dispatcher.Dispatch(new AddEntityCommand(GeometryType.Cube)).Data!;

        // Act
        _sut.SubmitScene(Scene(TwoEntities));

        // Assert
        _scene.HasEntity(oldId).Should().BeFalse();
        _scene.Entities.Keys.Should().BeEquivalentTo("sofa", "lamp");
    }

    [Fact]
    public void SubmitScene_SubmittedSceneVersion_IsIgnored()
    {
        // Arrange
        var versionBefore = _scene.Version;

        // Act
        _sut.SubmitScene(Scene("""{"scene_version": 999, "entities": []}"""));

        // Assert
        _scene.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void SubmitScene_UnknownComponentAndField_ReturnsWarnings()
    {
        // Arrange
        const string json = """
            {"entities": [{"id": "a", "components": {
              "transform": {"position": [0, 0, 0], "pivot": [0, 0, 0]},
              "mesh_filter": {"geometry": "Cube"},
              "mesh_renderer": {},
              "entity_name": {"value": "a"},
              "physics": {}}}]}
            """;

        // Act
        var node = ToolResponse.Parse(_sut.SubmitScene(Scene(json)));

        // Assert
        node["code"]!.GetValue<int>().Should().Be(200);
        var warnings = node["result"]!["warnings"]!.AsArray().Select(w => w!.GetValue<string>());
        warnings
            .Should()
            .HaveCount(2)
            .And.Contain(w => w.Contains("unknown field 'pivot'"))
            .And.Contain(w => w.Contains("unknown component type 'physics'"));
    }

    [Fact]
    public void SubmitScene_OutOfRangeValue_ReturnsBadRequestAndKeepsScene()
    {
        // Arrange
        _sut.SubmitScene(Scene(TwoEntities));
        var versionBefore = _scene.Version;
        const string json = """
            {"entities": [{"id": "a", "components": {
              "mesh_filter": {"geometry": "Cube"},
              "mesh_renderer": {"opacity": 1.5}}}]}
            """;

        // Act
        var node = ToolResponse.Parse(_sut.SubmitScene(Scene(json)));

        // Assert
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("mesh_renderer");
        _scene.Entities.Keys.Should().BeEquivalentTo("sofa", "lamp");
        _scene.Version.Should().Be(versionBefore);
    }

    [Fact]
    public void SubmitScene_DuplicateIds_ReturnsBadRequest()
    {
        // Arrange
        const string json = """
            {"entities": [
              {"id": "a", "components": {"mesh_filter": {"geometry": "Cube"}}},
              {"id": "a", "components": {"mesh_filter": {"geometry": "Sphere"}}}]}
            """;

        // Act
        var node = ToolResponse.Parse(_sut.SubmitScene(Scene(json)));

        // Assert
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("Duplicate entity id 'a'");
    }

    [Fact]
    public void SubmitScene_MissingEntities_ReturnsBadRequest()
    {
        // Act
        var node = ToolResponse.Parse(_sut.SubmitScene(Scene("""{"objects": []}""")));

        // Assert
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("'entities'");
    }

    [Fact]
    public void SubmitScene_MissingMeshFilter_ReturnsBadRequest()
    {
        // Act
        var node = ToolResponse.Parse(
            _sut.SubmitScene(Scene("""{"entities": [{"id": "a", "components": {}}]}"""))
        );

        // Assert
        node["code"]!.GetValue<int>().Should().Be(400);
        node["error"]!.GetValue<string>().Should().Contain("mesh_filter");
    }

    #endregion
}
