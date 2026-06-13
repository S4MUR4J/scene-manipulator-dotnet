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
        root.GetProperty("sceneVersion").GetInt64().Should().Be(scene.Version);
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
            .GetProperty("Transform")
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
            .GetProperty("MeshFilter")
            .GetProperty("geometry")
            .GetString();
        geometry.Should().Be("Sphere");
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
            .GetProperty("MeshRenderer");
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
            .GetProperty("EntityName")
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

    #region Deserialize

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
        result.Count.Should().Be(1);
        var entity = result.Entities.Values.First();
        entity.Get<Transform>()!.Position.Should().Be(Vector3.Up);
        entity.Get<MeshFilter>()!.Geometry.Should().Be(GeometryType.Cube);
        entity.Get<MeshRenderer>()!.Color.Should().Be("#abcdef");
        entity.Get<MeshRenderer>()!.Opacity.Should().Be(0.8f);
        entity.Get<EntityName>()!.Value.Should().Be("RoundTrip");
    }

    [Fact]
    public void Deserialize_EmptyScene_ReturnsEmptyScene()
    {
        // Arrange
        var json = """{"version":"1.0","sceneVersion":0,"entities":[]}""";

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        result.Count.Should().Be(0);
    }

    [Fact]
    public void Deserialize_UnknownComponentType_IsSkipped()
    {
        // Arrange
        var json = """
            {
              "version": "1.0",
              "sceneVersion": 1,
              "entities": [
                {
                  "id": "entity_1",
                  "components": {
                    "UnknownComponent": { "foo": "bar" },
                    "EntityName": { "value": "Test" }
                  }
                }
              ]
            }
            """;

        // Act
        var result = SceneSerializer.Deserialize(json);

        // Assert
        result.Count.Should().Be(1);
        var entity = result.Entities.Values.First();
        entity.Has<EntityName>().Should().BeTrue();
        entity.Has("UnknownComponent").Should().BeFalse();
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsSceneDeserializationException()
    {
        // Arrange
        var act = () => SceneSerializer.Deserialize("not valid json {{{");

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
              "sceneVersion": 2,
              "entities": [
                { "id": "entity_1", "components": {} },
                { "id": "entity_1", "components": {} }
              ]
            }
            """;

        // Act
        var act = () => SceneSerializer.Deserialize(json);

        // Assert
        act.Should().Throw<SceneDeserializationException>();
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
        var transform = result.Entities.Values.First().Get<Transform>()!;
        transform.Position.Should().Be(Vector3.Up);
        transform.Rotation.Should().Be(Vector3.Right);
        transform.Scale.Should().Be(Vector3.One);
    }

    [Fact]
    public void Deserialize_MeshFilter_RestoresGeometryType()
    {
        // Arrange
        var original = new SceneBuilder()
            .WithEntity(e => e.WithComponent(new MeshFilter(GeometryType.Pyramid, null)))
            .Build();

        // Act
        var result = SceneSerializer.Deserialize(SceneSerializer.Serialize(original));

        // Assert
        result
            .Entities.Values.First()
            .Get<MeshFilter>()!
            .Geometry.Should()
            .Be(GeometryType.Pyramid);
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
        var mr = result.Entities.Values.First().Get<MeshRenderer>()!;
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
        result.Entities.Values.First().Get<EntityName>()!.Value.Should().Be("MyEntity");
    }

    #endregion
}
