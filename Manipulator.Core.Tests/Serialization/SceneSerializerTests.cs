using System.Text.Json;
using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Serialization;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Serialization;

public class SceneSerializerTests
{
    #region Serialize

    [Fact]
    public void Serialize_EmptyScene_ProducesValidJson()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();
    }

    [Fact]
    public void Serialize_IncludesVersionAndSceneVersion()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;
        root.GetProperty("version").GetString().Should().Be("1.0");
        root.GetProperty("scene_version").GetInt64().Should().Be(scene.Version);
    }

    [Fact]
    public void Serialize_EntityWithTransform_WritesPositionAsArray()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new Transform { Position = Vector3.Right }))
            .Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var position = JsonDocument
            .Parse(json)
            .RootElement.GetProperty("entities")[0]
            .GetProperty("components")
            .GetProperty("transform")
            .GetProperty("position");
        position[0].GetSingle().Should().Be(1f);
        position[1].GetSingle().Should().Be(0f);
        position[2].GetSingle().Should().Be(0f);
    }

    [Fact]
    public void Serialize_EntityWithMeshFilter_WritesGeometryName()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new MeshFilter(GeometryType.Sphere, null)))
            .Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var geometry = JsonDocument
            .Parse(json)
            .RootElement.GetProperty("entities")[0]
            .GetProperty("components")
            .GetProperty("mesh_filter")
            .GetProperty("geometry")
            .GetString();
        geometry.Should().Be("Sphere");
    }

    [Fact]
    public void Serialize_EntityWithMeshFilterParameters_WritesParameters()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e =>
                e.WithComponent(
                    new MeshFilter(
                        GeometryType.Sphere,
                        new Dictionary<string, object> { ["radius"] = 2.5f }
                    )
                )
            )
            .Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var parameters = JsonDocument
            .Parse(json)
            .RootElement.GetProperty("entities")[0]
            .GetProperty("components")
            .GetProperty("mesh_filter")
            .GetProperty("parameters");
        parameters.GetProperty("radius").GetSingle().Should().Be(2.5f);
    }

    [Fact]
    public void Serialize_EntityWithMeshRenderer_WritesAllFields()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e =>
                e.WithComponent(
                    new MeshRenderer(
                        Color: "#ff0000",
                        Opacity: 0.5f,
                        Metalness: 0.1f,
                        Roughness: 0.9f
                    )
                )
            )
            .Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var mr = JsonDocument
            .Parse(json)
            .RootElement.GetProperty("entities")[0]
            .GetProperty("components")
            .GetProperty("mesh_renderer");
        mr.GetProperty("color").GetString().Should().Be("#ff0000");
        mr.GetProperty("opacity").GetSingle().Should().Be(0.5f);
        mr.GetProperty("metalness").GetSingle().Should().Be(0.1f);
        mr.GetProperty("roughness").GetSingle().Should().Be(0.9f);
    }

    [Fact]
    public void Serialize_EntityWithEntityName_WritesValue()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new EntityName("Cube")))
            .Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var value = JsonDocument
            .Parse(json)
            .RootElement.GetProperty("entities")[0]
            .GetProperty("components")
            .GetProperty("entity_name")
            .GetProperty("value")
            .GetString();
        value.Should().Be("Cube");
    }

    [Fact]
    public void Serialize_MultipleEntities_AllIncluded()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntities(3).Build();

        // Act
        var json = SceneSerializer.Serialize(scene);

        // Assert
        var entities = JsonDocument.Parse(json).RootElement.GetProperty("entities");
        entities.GetArrayLength().Should().Be(3);
    }

    #endregion

    #region Deserialize - success

    [Fact]
    public void Deserialize_RoundTrip_ProducesLogicallyIdenticalScene()
    {
        // Arrange
        var original = new SceneBuilder()
            .WithEntity(e =>
                e.WithComponent(new Transform { Position = Vector3.Up })
                    .WithComponent(new MeshFilter(GeometryType.Cube, null))
                    .WithComponent(
                        new MeshRenderer(
                            Color: "#abcdef",
                            Opacity: 0.8f,
                            Metalness: 0.2f,
                            Roughness: 0.6f
                        )
                    )
                    .WithComponent(new EntityName("RoundTrip"))
            )
            .Build();

        // Act
        var result = SceneSerializer.Deserialize(SceneSerializer.Serialize(original));

        // Assert
        result.Scene.Count.Should().Be(1);
        var entity = result.Scene.Entities.Values.First();
        entity.Get<Transform>()!.Position.Should().Be(Vector3.Up);
        entity.Get<MeshFilter>()!.Geometry.Should().Be(GeometryType.Cube);
        entity.Get<MeshRenderer>()!.Color.Should().Be("#abcdef");
        entity.Get<MeshRenderer>()!.Opacity.Should().Be(0.8f);
        entity.Get<EntityName>()!.Value.Should().Be("RoundTrip");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_EmptyScene_ReturnsEmptyScene()
    {
        // Arrange
        var json = """{"version":"1.0","scene_version":0,"entities":[]}""";

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        result.Scene.Count.Should().Be(0);
    }

    [Fact]
    public void Deserialize_RestoresSceneVersion_NotEntityCount()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 42,
              "entities": [
                { "id": "entity_1", "components": {} }
              ]
            }
            """;

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        result.Scene.Version.Should().Be(42);
    }

    [Fact]
    public void Deserialize_UnknownComponentType_IsSkippedAndReportedAsWarning()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                {
                  "id": "entity_1",
                  "components": {
                    "unknown_component": { "foo": "bar" },
                    "entity_name": { "value": "Test" }
                  }
                }
              ]
            }
            """;

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        var entity = result.Scene.Entities.Values.First();
        entity.Has<EntityName>().Should().BeTrue();
        entity.Has("UnknownComponent").Should().BeFalse();
        result.Warnings.Should().ContainSingle(w => w.Contains("unknown_component"));
    }

    [Fact]
    public void Deserialize_UnknownFieldOnKnownComponent_IsIgnoredAndReportedAsWarning()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                {
                  "id": "entity_1",
                  "components": {
                    "entity_name": { "value": "Test", "nickname": "extra" }
                  }
                }
              ]
            }
            """;

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        result.Scene.Entities.Values.First().Get<EntityName>()!.Value.Should().Be("Test");
        result.Warnings.Should().ContainSingle(w => w.Contains("nickname"));
    }

    [Fact]
    public void Deserialize_MeshFilterParameters_RoundTrips()
    {
        // Arrange
        var original = new SceneBuilder()
            .WithEntity(e =>
                e.WithComponent(
                    new MeshFilter(
                        GeometryType.Sphere,
                        new Dictionary<string, object> { ["radius"] = 1.5f, ["segments"] = 32 }
                    )
                )
            )
            .Build();

        // Act
        var result = SceneSerializer.Deserialize(SceneSerializer.Serialize(original));

        // Assert
        var parameters = result.Scene.Entities.Values.First().Get<MeshFilter>()!.Parameters;
        parameters.Should().NotBeNull();
        ((JsonElement)parameters!["radius"]).GetDouble().Should().Be(1.5);
        ((JsonElement)parameters["segments"]).GetInt32().Should().Be(32);
    }

    [Fact]
    public void Deserialize_GeometryNameIsCaseInsensitive()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                { "id": "entity_1", "components": { "mesh_filter": { "geometry": "sphere" } } }
              ]
            }
            """;

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        result
            .Scene.Entities.Values.First()
            .Get<MeshFilter>()!
            .Geometry.Should()
            .Be(GeometryType.Sphere);
    }

    [Fact]
    public void Deserialize_TransformOmitted_UsesComponentDefaults()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                { "id": "entity_1", "components": { "transform": {} } }
              ]
            }
            """;

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        var transform = result.Scene.Entities.Values.First().Get<Transform>()!;
        transform.Position.Should().Be(Vector3.Zero);
        transform.Rotation.Should().Be(Vector3.Zero);
        transform.Scale.Should().Be(Vector3.One);
    }

    [Fact]
    public void Deserialize_MeshRendererOmittedFields_UsesComponentDefaults()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                { "id": "entity_1", "components": { "mesh_renderer": {} } }
              ]
            }
            """;

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        var meshRenderer = result.Scene.Entities.Values.First().Get<MeshRenderer>()!;
        meshRenderer.Color.Should().Be("#ffffff");
        meshRenderer.Opacity.Should().Be(1.0f);
        meshRenderer.Metalness.Should().Be(0.0f);
        meshRenderer.Roughness.Should().Be(0.5f);
    }

    [Fact]
    public void Deserialize_Transform_RestoresAllVectors()
    {
        // Arrange
        var original = new SceneBuilder()
            .WithEntity(e =>
                e.WithComponent(
                    new Transform
                    {
                        Position = Vector3.Up,
                        Rotation = Vector3.Right,
                        Scale = Vector3.One,
                    }
                )
            )
            .Build();

        // Act
        var result = SceneSerializer.Deserialize(SceneSerializer.Serialize(original));

        // Assert
        var transform = result.Scene.Entities.Values.First().Get<Transform>()!;
        transform.Position.Should().Be(Vector3.Up);
        transform.Rotation.Should().Be(Vector3.Right);
        transform.Scale.Should().Be(Vector3.One);
    }

    [Fact]
    public void Deserialize_MeshFilter_RestoresGeometryType()
    {
        // Arrange
        var original = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new MeshFilter(GeometryType.Cone, null)))
            .Build();

        // Act
        var result = SceneSerializer.Deserialize(SceneSerializer.Serialize(original));

        // Assert
        result
            .Scene.Entities.Values.First()
            .Get<MeshFilter>()!
            .Geometry.Should()
            .Be(GeometryType.Cone);
    }

    [Fact]
    public void Deserialize_MeshRenderer_RestoresAllFields()
    {
        // Arrange
        var original = new SceneBuilder()
            .WithEntity(e =>
                e.WithComponent(
                    new MeshRenderer(
                        Color: "#123456",
                        Opacity: 0.3f,
                        Metalness: 0.7f,
                        Roughness: 0.4f
                    )
                )
            )
            .Build();

        // Act
        var result = SceneSerializer.Deserialize(SceneSerializer.Serialize(original));

        // Assert
        var mr = result.Scene.Entities.Values.First().Get<MeshRenderer>()!;
        mr.Color.Should().Be("#123456");
        mr.Opacity.Should().Be(0.3f);
        mr.Metalness.Should().Be(0.7f);
        mr.Roughness.Should().Be(0.4f);
    }

    [Fact]
    public void Deserialize_EntityName_RestoresValue()
    {
        // Arrange
        var original = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new EntityName("MyEntity")))
            .Build();

        // Act
        var result = SceneSerializer.Deserialize(SceneSerializer.Serialize(original));

        // Assert
        result.Scene.Entities.Values.First().Get<EntityName>()!.Value.Should().Be("MyEntity");
    }

    [Fact]
    public void Deserialize_Fixture_RoundTripsWithoutWarnings()
    {
        // Arrange
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "scene.sample.json");
        var json = File.ReadAllText(path);

        // Act
        var first = SceneSerializer.Deserialize(json);
        var reserialized = SceneSerializer.Serialize(first.Scene);
        var second = SceneSerializer.Deserialize(reserialized);

        // Assert
        first.Warnings.Should().BeEmpty();
        first.Scene.Count.Should().Be(2);
        first.Scene.Version.Should().Be(2);
        second.Scene.Count.Should().Be(first.Scene.Count);
        second.Scene.Version.Should().Be(first.Scene.Version);
        SceneSerializer.Serialize(second.Scene).Should().Be(reserialized);
    }

    #endregion

    #region Deserialize - failure

    [Fact]
    public void Deserialize_InvalidJson_ThrowsSceneDeserializationException()
    {
        // Arrange
        var act = () => SceneSerializer.Deserialize("not valid json {{{");

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>();
    }

    [Fact]
    public void Deserialize_NullJson_ThrowsSceneDeserializationException()
    {
        // Arrange
        var act = () => SceneSerializer.Deserialize("null");

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>();
    }

    [Fact]
    public void Deserialize_MissingEntities_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """{"version":"1.0","scene_version":0}""";
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*entities*");
    }

    [Fact]
    public void Deserialize_MissingEntityId_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [ { "components": {} } ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*id*");
    }

    [Fact]
    public void Deserialize_MissingComponents_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [ { "id": "entity_1" } ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should()
            .Throw<SceneDeserializationException>()
            .WithMessage("*entity_1*")
            .WithMessage("*components*");
    }

    [Fact]
    public void Deserialize_BlankEntityId_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [ { "id": "   ", "components": {} } ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>();
    }

    [Fact]
    public void Deserialize_DuplicateEntityId_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 2,
              "entities": [
                { "id": "entity_1", "components": {} },
                { "id": "entity_1", "components": {} }
              ]
            }
            """;

        // Act
        var act = () => SceneSerializer.Deserialize(json);

        // Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*entity_1*");
    }

    [Theory]
    [InlineData("""[0, 1]""")]
    [InlineData("""[0, 1, 2, 3]""")]
    public void Deserialize_TransformVectorWithWrongLength_ThrowsSceneDeserializationException(
        string positionArray
    )
    {
        // Arrange
        var json = $$"""
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                { "id": "entity_1", "components": { "transform": { "position": {{positionArray}} } } }
              ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should()
            .Throw<SceneDeserializationException>()
            .WithMessage("*entity_1*")
            .WithMessage("*position*");
    }

    [Fact]
    public void Deserialize_TransformVectorNonFinite_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                {
                  "id": "entity_1",
                  "components": { "transform": { "position": [1e400, 0, 0] } }
                }
              ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*position*");
    }

    [Fact]
    public void Deserialize_MeshFilterMissingGeometry_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                { "id": "entity_1", "components": { "mesh_filter": {} } }
              ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*geometry*");
    }

    [Fact]
    public void Deserialize_MeshFilterUnknownGeometry_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                {
                  "id": "entity_1",
                  "components": { "mesh_filter": { "geometry": "Dodecahedron" } }
                }
              ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*Dodecahedron*");
    }

    [Fact]
    public void Deserialize_MeshRendererInvalidColor_ThrowsSceneDeserializationException()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                {
                  "id": "entity_1",
                  "components": { "mesh_renderer": { "color": "red" } }
                }
              ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*entity_1*");
    }

    [Theory]
    [InlineData("opacity", 1.5)]
    [InlineData("metalness", -0.1)]
    [InlineData("roughness", 2)]
    public void Deserialize_MeshRendererValueOutOfRange_ThrowsSceneDeserializationException(
        string field,
        double value
    )
    {
        // Arrange
        var valueText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var json = $$"""
            {
              "version": "1.0",
              "scene_version": 1,
              "entities": [
                {
                  "id": "entity_1",
                  "components": { "mesh_renderer": { "{{field}}": {{valueText}} } }
                }
              ]
            }
            """;
        var act = () => SceneSerializer.Deserialize(json);

        // Act & Assert
        act.Should().Throw<SceneDeserializationException>().WithMessage("*entity_1*");
    }

    #endregion
}
