using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Events;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Handlers;

public class ReplaceSceneHandlerTests
{
    private readonly Scene _scene = new SceneBuilder()
        .WithEntity(SceneBuilder.Id(1, "old"), WithCube)
        .WithEntity(SceneBuilder.Id(2, "old"), WithCube)
        .Build();

    private readonly ReplaceSceneHandler _handler = new ReplaceSceneHandler();

    private static void WithCube(EntityBuilder builder) =>
        builder
            .WithComponent(new Transform())
            .WithComponent(new MeshFilter(GeometryType.Cube, null))
            .WithComponent(new MeshRenderer())
            .WithComponent(new EntityName("cube"));

    private static Scene Replacement(Action<EntityBuilder>? configure = null) =>
        new SceneBuilder().WithEntity(SceneBuilder.Id(1, "new"), configure ?? WithCube).Build();

    [Fact]
    public void Handle_ValidScene_ReplacesEntitiesKeepingGivenIds()
    {
        // Act
        var result = _handler.Handle(_scene, new ReplaceSceneCommand(Replacement()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        _scene.Entities.Keys.Should().BeEquivalentTo(SceneBuilder.Id(1, "new"));
    }

    [Fact]
    public void Handle_ValidScene_BumpsVersionByOne()
    {
        // Arrange
        var versionBefore = _scene.Version;

        // Act
        _handler.Handle(_scene, new ReplaceSceneCommand(Replacement()));

        // Assert
        _scene.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Handle_ValidScene_ReturnsSceneReplacedEvent()
    {
        // Act
        var result = _handler.Handle(_scene, new ReplaceSceneCommand(Replacement()));

        // Assert
        result
            .Events.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<SceneReplacedEvent>()
            .Which.EntityIds.Should()
            .Equal(SceneBuilder.Id(1, "new"));
    }

    [Fact]
    public void Handle_ReplacedEntityChanges_BumpVersionOfTargetScene()
    {
        // Arrange
        _handler.Handle(_scene, new ReplaceSceneCommand(Replacement()));
        var versionBefore = _scene.Version;

        // Act
        _scene.GetEntity(SceneBuilder.Id(1, "new"))!.Set(new EntityName("renamed"));

        // Assert
        _scene.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Handle_EmptyScene_ClearsAllEntities()
    {
        // Act
        var result = _handler.Handle(_scene, new ReplaceSceneCommand(new Scene()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        _scene.Count.Should().Be(0);
    }

    [Fact]
    public void Handle_MissingMeshFilter_ReturnsFailAndLeavesSceneUnchanged()
    {
        // Arrange
        var versionBefore = _scene.Version;
        var replacement = Replacement(e => e.WithComponent(new EntityName("no geometry")));

        // Act
        var result = _handler.Handle(_scene, new ReplaceSceneCommand(replacement));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("mesh_filter");
        _scene
            .Entities.Keys.Should()
            .BeEquivalentTo(SceneBuilder.Id(1, "old"), SceneBuilder.Id(2, "old"));
        _scene.Version.Should().Be(versionBefore);
    }

    [Theory]
    [InlineData(0f, 1f, 1f)]
    [InlineData(1f, -2f, 1f)]
    [InlineData(1f, 1f, 0f)]
    public void Handle_NonPositiveScale_ReturnsFail(float x, float y, float z)
    {
        // Arrange
        var replacement = Replacement(e =>
            e.WithComponent(new Transform { Scale = new Vector3(x, y, z) })
                .WithComponent(new MeshFilter(GeometryType.Cube, null))
        );

        // Act
        var result = _handler.Handle(_scene, new ReplaceSceneCommand(replacement));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("scale must be positive");
        _scene.Count.Should().Be(2);
    }

    [Fact]
    public void Handle_MissingOptionalComponents_FillsDefaultsAndWarns()
    {
        // Arrange
        var replacement = Replacement(e =>
            e.WithComponent(new MeshFilter(GeometryType.Sphere, null))
        );

        // Act
        var result = _handler.Handle(_scene, new ReplaceSceneCommand(replacement));

        // Assert
        var entity = _scene.GetEntity(SceneBuilder.Id(1, "new"))!;
        entity.Get<Transform>().Should().Be(new Transform());
        entity.Get<MeshRenderer>().Should().Be(new MeshRenderer());
        entity.Get<EntityName>().Should().Be(new EntityName());
        result
            .Data.Should()
            .BeAssignableTo<IReadOnlyList<string>>()
            .Which.Should()
            .HaveCount(3)
            .And.Contain(w => w.Contains("'transform'"))
            .And.Contain(w => w.Contains("'mesh_renderer'"))
            .And.Contain(w => w.Contains("'entity_name'"));
    }

    [Fact]
    public void Handle_CompleteEntities_ReturnsNoWarnings()
    {
        // Act
        var result = _handler.Handle(_scene, new ReplaceSceneCommand(Replacement()));

        // Assert
        result.Data.Should().BeAssignableTo<IReadOnlyList<string>>().Which.Should().BeEmpty();
    }
}
