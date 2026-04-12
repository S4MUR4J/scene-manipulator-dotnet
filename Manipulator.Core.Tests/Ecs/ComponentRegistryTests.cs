using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;

namespace Manipulator.Core.Tests.Ecs;

public class ComponentRegistryTests
{
    #region Register (generic)

    [Fact]
    public void Register_Generic_StoresType_ByComponentTypeName()
    {
        // Arrange
        var registry = new ComponentRegistry();

        // Act
        registry.Register<Transform>();

        // Assert
        registry.Resolve("Transform").Should().Be(typeof(Transform));
    }

    [Fact]
    public void Register_Generic_OverwritesExisting_WhenRegisteredTwice()
    {
        // Arrange
        var registry = new ComponentRegistry();
        registry.Register<Transform>();

        // Act
        registry.Register<Transform>();

        // Assert
        registry.Resolve("Transform").Should().Be(typeof(Transform));
    }

    #endregion

    #region Register (string, Type)

    [Fact]
    public void Register_StringType_StoresType_WhenValidIComponent()
    {
        // Arrange
        var registry = new ComponentRegistry();

        // Act
        registry.Register("MyTransform", typeof(Transform));

        // Assert
        registry.Resolve("MyTransform").Should().Be(typeof(Transform));
    }

    [Fact]
    public void Register_StringType_ThrowsArgumentException_WhenTypeDoesNotImplementIComponent()
    {
        // Arrange
        var registry = new ComponentRegistry();

        // Act
        Action act = () => registry.Register("Invalid", typeof(string));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Resolve

    [Fact]
    public void Resolve_ReturnsType_WhenRegistered()
    {
        // Arrange
        var registry = new ComponentRegistry();
        registry.Register<Transform>();

        // Act
        var result = registry.Resolve("Transform");

        // Assert
        result.Should().Be(typeof(Transform));
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNotRegistered()
    {
        // Arrange
        var registry = new ComponentRegistry();

        // Act
        var result = registry.Resolve("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsRegistered

    [Fact]
    public void IsRegistered_ReturnsTrue_WhenTypeRegistered()
    {
        // Arrange
        var registry = new ComponentRegistry();
        registry.Register<Transform>();

        // Act
        var result = registry.IsRegistered("Transform");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_ReturnsFalse_WhenTypeNotRegistered()
    {
        // Arrange
        var registry = new ComponentRegistry();

        // Act
        var result = registry.IsRegistered("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RegisteredTypes

    [Fact]
    public void RegisteredTypes_ReturnsEmpty_WhenNothingRegistered()
    {
        // Arrange
        var registry = new ComponentRegistry();

        // Act
        var result = registry.RegisteredTypes;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RegisteredTypes_ContainsAllRegisteredTypeNames()
    {
        // Arrange
        var registry = new ComponentRegistry();
        registry.Register<Transform>();
        registry.Register<EntityName>();

        // Act
        var result = registry.RegisteredTypes;

        // Assert
        result.Should().BeEquivalentTo(["Transform", "EntityName"]);
    }

    #endregion
}