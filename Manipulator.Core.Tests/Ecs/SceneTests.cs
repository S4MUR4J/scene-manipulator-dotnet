using FluentAssertions;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Ecs;

public class SceneTests
{
    #region AddEntity

    [Fact]
    public void AddEntity_ReturnsAddedEntity()
    {
        // Arrange
        var scene = new SceneBuilder().Build();
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var result = scene.AddEntity(entity);

        // Assert
        result.Should().Be(entity);
    }

    [Fact]
    public void AddEntity_AppearsInEntities()
    {
        // Arrange
        var scene = new SceneBuilder().Build();
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        scene.AddEntity(entity);

        // Assert
        scene.Entities.Should().ContainKey(SceneBuilder.Id(1));
        scene.Entities[SceneBuilder.Id(1)].Should().Be(entity);
    }

    [Fact]
    public void AddEntity_OverwritesEntityWithSameId()
    {
        // Arrange
        var scene = new SceneBuilder().Build();
        var firstEntity = new EntityBuilder(SceneBuilder.Id(1)).Build();
        var secondEntity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        scene.AddEntity(firstEntity);
        scene.AddEntity(secondEntity);

        // Assert
        scene.Entities[SceneBuilder.Id(1)].Should().Be(secondEntity);
        scene.Count.Should().Be(1);
    }

    #endregion

    #region RemoveEntity

    [Fact]
    public void RemoveEntity_ExistingId_ReturnsTrueAndRemoves()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = scene.RemoveEntity(SceneBuilder.Id(1));

        // Assert
        result.Should().BeTrue();
        scene.HasEntity(SceneBuilder.Id(1)).Should().BeFalse();
    }

    [Fact]
    public void RemoveEntity_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var result = scene.RemoveEntity(SceneBuilder.Id(99));

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllEntities()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntities(2).Build();

        // Act
        scene.Clear();

        // Assert
        scene.Count.Should().Be(0);
        scene.Entities.Should().BeEmpty();
    }

    [Fact]
    public void Clear_OnEmptyScene_DoesNotThrow()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var act = () => scene.Clear();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Clear_CalledTwice_StaysEmpty()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        scene.Clear();
        scene.Clear();

        // Assert
        scene.Count.Should().Be(0);
    }

    #endregion

    #region GetEntity / HasEntity

    [Fact]
    public void GetEntity_ExistingId_ReturnsEntity()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();
        var scene = new SceneBuilder().Build();
        scene.AddEntity(entity);

        // Act
        var result = scene.GetEntity(SceneBuilder.Id(1));

        // Assert
        result.Should().Be(entity);
    }

    [Fact]
    public void GetEntity_NonExistingId_ReturnsNull()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var result = scene.GetEntity(SceneBuilder.Id(99));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void HasEntity_ExistingId_ReturnsTrue()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntity().Build();

        // Act
        var result = scene.HasEntity(SceneBuilder.Id(1));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasEntity_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var result = scene.HasEntity(SceneBuilder.Id(99));

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Count

    [Fact]
    public void Count_EmptyScene_IsZero()
    {
        // Arrange
        var scene = new SceneBuilder().Build();

        // Act
        var result = scene.Count;

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Count_AfterAddingEntities_ReflectsTotal()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntities(3).Build();

        // Act
        var result = scene.Count;

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void Count_AfterRemove_Decrements()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntities(2).Build();

        // Act
        scene.RemoveEntity(SceneBuilder.Id(1));

        // Assert
        scene.Count.Should().Be(1);
    }

    [Fact]
    public void Count_AfterClear_IsZero()
    {
        // Arrange
        var scene = new SceneBuilder().WithEntities(2).Build();

        // Act
        scene.Clear();

        // Assert
        scene.Count.Should().Be(0);
    }

    #endregion
}
