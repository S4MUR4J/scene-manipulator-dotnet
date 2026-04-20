using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Ecs;

public class EntityTests
{
    #region Constructor

    [Theory]
    [InlineData("player", "player")]
    [InlineData("  player  ", "player")]
    [InlineData("a", "a")]
    public void Constructor_ValidId_SetsId(string input, string expectedId)
    {
        // Act
        var entity = new Entity(input);

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullId = null;

        // Act
        var act = () => new Entity(nullId!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_EmptyOrWhitespaceId_ThrowsArgumentException(string id)
    {
        // Act
        var act = () => new Entity(id);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Get<T>

    [Fact]
    public void Get_Generic_ReturnsTransform_WhenSet()
    {
        // Arrange
        var transform = new Transform { Position = new Vector3(1, 2, 3) };
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(transform).Build();

        // Act
        var result = entity.Get<Transform>();

        // Assert
        result.Should().Be(transform);
    }

    [Fact]
    public void Get_Generic_ReturnsEntityName_WhenSet()
    {
        // Arrange
        var name = new EntityName("Player");
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(name).Build();

        // Act
        var result = entity.Get<EntityName>();

        // Assert
        result.Should().Be(name);
    }

    [Fact]
    public void Get_Generic_ReturnsMeshFilter_WhenSet()
    {
        // Arrange
        var meshFilter = new MeshFilter(GeometryType.Sphere, null);
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(meshFilter).Build();

        // Act
        var result = entity.Get<MeshFilter>();

        // Assert
        result.Should().Be(meshFilter);
    }

    [Fact]
    public void Get_Generic_ReturnsMeshRenderer_WhenSet()
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Color: "#FF0000", Opacity: 0.8f);
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(meshRenderer).Build();

        // Act
        var result = entity.Get<MeshRenderer>();

        // Assert
        result.Should().Be(meshRenderer);
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

    #endregion

    #region Get(string)

    [Fact]
    public void Get_ByTypeName_ReturnsComponent_WhenSet()
    {
        // Arrange
        var transform = new Transform();
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(transform).Build();

        // Act
        var result = entity.Get("Transform");

        // Assert
        result.Should().Be(transform);
    }

    [Fact]
    public void Get_ByTypeName_ReturnsNull_WhenNotSet()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var result = entity.Get("Transform");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("transform")]
    [InlineData("TRANSFORM")]
    public void Get_ByTypeName_ReturnsNull_WhenKeyIsUnknown(string typeName)
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(new Transform()).Build();

        // Act
        var result = entity.Get(typeName);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Has<T>

    [Fact]
    public void Has_Generic_ReturnsTrue_WhenComponentExists()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(new Transform()).Build();

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

    #endregion

    #region Has(string)

    [Fact]
    public void Has_ByTypeName_ReturnsTrue_WhenComponentExists()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(new Transform()).Build();

        // Act
        var result = entity.Has("Transform");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Has_ByTypeName_ReturnsFalse_WhenComponentMissing()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var result = entity.Has("Transform");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("transform")]
    [InlineData("Transform1")]
    public void Has_ByTypeName_ReturnsFalse_WhenKeyIsUnknown(string typeName)
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(new Transform()).Build();

        // Act
        var result = entity.Has(typeName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Components

    [Fact]
    public void Components_ReturnsAllSetComponents()
    {
        // Arrange
        var transform = new Transform();
        var name = new EntityName("Player");
        var entity = new EntityBuilder(SceneBuilder.Id(1))
            .WithComponent(transform)
            .WithComponent(name)
            .Build();

        // Act
        var components = entity.Components;

        // Assert
        components.Should().HaveCount(2);
        components.Should().ContainKey("Transform");
        components.Should().ContainKey("EntityName");
    }

    [Fact]
    public void Components_IsReadOnly_MutationThrowsNotSupportedException()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var act = () =>
            ((IDictionary<string, IComponent>)entity.Components).Add("Transform", new Transform());

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    #endregion

    #region Set<T>

    [Fact]
    public void Set_Generic_AddsNewComponent()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();
        var transform = new Transform();

        // Act
        entity.Set(transform);

        // Assert
        entity.Get<Transform>().Should().Be(transform);
    }

    [Fact]
    public void Set_Generic_OverwritesExistingComponent()
    {
        // Arrange
        var firstTransform = new Transform { Position = new Vector3(1, 0, 0) };
        var secondTransform = new Transform { Position = new Vector3(2, 0, 0) };
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(firstTransform).Build();

        // Act
        entity.Set(secondTransform);

        // Assert
        entity.Get<Transform>().Should().Be(secondTransform);
    }

    [Fact]
    public void Set_Generic_MultipleTypes_AllPresent()
    {
        // Arrange
        var transform = new Transform();
        var name = new EntityName("Player");
        var meshFilter = new MeshFilter(GeometryType.Cube, null);
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        entity.Set(transform);
        entity.Set(name);
        entity.Set(meshFilter);

        // Assert
        entity.Has<Transform>().Should().BeTrue();
        entity.Has<EntityName>().Should().BeTrue();
        entity.Has<MeshFilter>().Should().BeTrue();
    }

    #endregion

    #region Set(string, IComponent)

    [Fact]
    public void Set_ByTypeName_AddsComponent()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();
        var transform = new Transform();

        // Act
        entity.Set("Transform", transform);

        // Assert
        entity.Get("Transform").Should().Be(transform);
    }

    [Fact]
    public void Set_ByTypeName_OverwritesExistingComponent()
    {
        // Arrange
        var firstTransform = new Transform { Position = new Vector3(0, 1, 0) };
        var secondTransform = new Transform { Position = new Vector3(0, 2, 0) };
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(firstTransform).Build();

        // Act
        entity.Set("Transform", secondTransform);

        // Assert
        entity.Get("Transform").Should().Be(secondTransform);
    }

    #endregion

    #region Remove<T>

    [Fact]
    public void Remove_Generic_ExistingComponent_ReturnsTrueAndRemoves()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(new Transform()).Build();

        // Act
        var result = entity.Remove<Transform>();

        // Assert
        result.Should().BeTrue();
        entity.Has<Transform>().Should().BeFalse();
    }

    [Fact]
    public void Remove_Generic_MissingComponent_ReturnsFalse()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var result = entity.Remove<Transform>();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Remove_Generic_DoesNotAffectOtherComponents()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1))
            .WithComponent(new Transform())
            .WithComponent(new EntityName("Player"))
            .Build();

        // Act
        entity.Remove<Transform>();

        // Assert
        entity.Has<EntityName>().Should().BeTrue();
    }

    #endregion

    #region Remove(string)

    [Fact]
    public void Remove_ByTypeName_ExistingComponent_ReturnsTrueAndRemoves()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(new Transform()).Build();

        // Act
        var result = entity.Remove("Transform");

        // Assert
        result.Should().BeTrue();
        entity.Has("Transform").Should().BeFalse();
    }

    [Fact]
    public void Remove_ByTypeName_MissingComponent_ReturnsFalse()
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

        // Act
        var result = entity.Remove("Transform");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("transform")]
    public void Remove_ByTypeName_UnknownKey_ReturnsFalse(string typeName)
    {
        // Arrange
        var entity = new EntityBuilder(SceneBuilder.Id(1)).WithComponent(new Transform()).Build();

        // Act
        var result = entity.Remove(typeName);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
