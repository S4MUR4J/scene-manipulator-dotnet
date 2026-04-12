using FluentAssertions;
using Manipulator.Core.Ecs.Components;
using static Manipulator.Core.Tests.Helpers.TestUtils;

namespace Manipulator.Core.Tests.Ecs.Components;

public class EntityNameTests
{
    #region Defaults

    [Fact]
    public void Constructor_Default_ValueIsEmpty()
    {
        // Act
        var entityName = new EntityName();

        // Assert
        entityName.Value.Should().Be(string.Empty);
    }

    #endregion

    #region Value

    [Fact]
    public void Constructor_CustomValue_StoresValue()
    {
        // Arrange
        var value = Tag("entity", 1);

        // Act
        var entityName = new EntityName(value);

        // Assert
        entityName.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_EmptyString_IsValid()
    {
        // Act
        var act = () => new EntityName("");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Type

    [Fact]
    public void Type_ReturnsEntityName()
    {
        // Arrange
        var entityName = new EntityName();

        // Act
        var result = entityName.Type;

        // Assert
        result.Should().Be(nameof(EntityName));
    }

    #endregion
}