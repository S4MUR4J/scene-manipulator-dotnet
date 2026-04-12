using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Ecs;

public class EntityTests
{
    [Fact]
    public void Get_Generic_ReturnsComponent_WhenSet()
    {
        // Arrange
        var transform = new Transform();
        var entity = new EntityBuilder(SceneBuilder.Id(1))
            .WithComponent(transform)
            .Build();

        // Act
        var result = entity.Get<Transform>();

        // Assert
        result.Should().Be(transform);
    }

    [Fact]
    public void Get_Generic_ReturnsNull_WhenNotSet()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var result = entity.Get<Transform>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Has_Generic_ReturnsTrue_WhenComponentExists()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1))
            .WithComponent(new Transform())
            .Build();

        // Act
        var result = entity.Has<Transform>();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Has_Generic_ReturnsFalse_WhenComponentMissing()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var result = entity.Has<Transform>();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Remove_Generic_RemovesComponent_AndReturnsTrue()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1))
            .WithComponent(new Transform())
            .Build();

        // Act
        var removed = entity.Remove<Transform>();

        // Assert
        removed.Should().BeTrue();
        entity.Has<Transform>().Should().BeFalse();
    }

    [Fact]
    public void Remove_Generic_ReturnsFalse_WhenComponentMissing()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var removed = entity.Remove<Transform>();

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public void Set_OverwritesExistingComponent()
    {
        // Arrange
        var firstTransform = new Transform(Position: new Vector3(1, 0, 0));
        var secondTransform = new Transform(Position: new Vector3(2, 0, 0));
        var entity = new EntityBuilder(SceneBuilder.Id(1))
            .WithComponent(firstTransform)
            .Build();

        // Act
        entity.Set(secondTransform);

        // Assert
        entity.Get<Transform>().Should().Be(secondTransform);
    }
}
