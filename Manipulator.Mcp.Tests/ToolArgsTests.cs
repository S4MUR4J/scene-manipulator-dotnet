using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp.Tests;

public class ToolArgsTests
{
    #region TryGeometry

    [Theory]
    [InlineData("Cube", GeometryType.Cube)]
    [InlineData("sphere", GeometryType.Sphere)]
    public void TryGeometry_KnownValue_ReturnsTrueWithParsedGeometry(
        string value,
        GeometryType expected
    )
    {
        // Act
        var success = ToolArgs.TryGeometry(value, out var geometry, out var error);

        // Assert
        success.Should().BeTrue();
        geometry.Should().Be(expected);
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGeometry_NullOrWhitespace_ReturnsFalseWithError(string? value)
    {
        // Act
        var success = ToolArgs.TryGeometry(value, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("is required");
    }

    [Fact]
    public void TryGeometry_UnknownValue_ReturnsFalseWithError()
    {
        // Act
        var success = ToolArgs.TryGeometry("Blob", out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Unknown geometry 'Blob'");
    }

    #endregion

    #region TryVector

    [Fact]
    public void TryVector_ValuesAreNull_ReturnsTrueWithNullVector()
    {
        // Act
        var success = ToolArgs.TryVector(null, "position", out var vector, out var error);

        // Assert
        success.Should().BeTrue();
        vector.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryVector_ValidValues_ReturnsTrueWithVector()
    {
        // Act
        var success = ToolArgs.TryVector([1, 2, 3], "position", out var vector, out var error);

        // Assert
        success.Should().BeTrue();
        vector.Should().Be(new Vector3(1, 2, 3));
        error.Should().BeNull();
    }

    [Fact]
    public void TryVector_WrongLength_ReturnsFalseWithError()
    {
        // Act
        var success = ToolArgs.TryVector([1, 2], "position", out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("exactly 3 numbers");
    }

    [Fact]
    public void TryVector_ContainsNonFiniteValue_ReturnsFalseWithError()
    {
        // Act
        var success = ToolArgs.TryVector([float.NaN, 0, 0], "position", out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("finite numbers");
    }

    #endregion

    #region TryRequiredVector

    [Fact]
    public void TryRequiredVector_ValuesAreNull_ReturnsFalseWithError()
    {
        // Act
        var success = ToolArgs.TryRequiredVector(null, "position", out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("is required");
    }

    [Fact]
    public void TryRequiredVector_ValidValues_ReturnsTrueWithVector()
    {
        // Act
        var success = ToolArgs.TryRequiredVector(
            [1, 2, 3],
            "position",
            out var vector,
            out var error
        );

        // Assert
        success.Should().BeTrue();
        vector.Should().Be(new Vector3(1, 2, 3));
        error.Should().BeNull();
    }

    #endregion

    #region TryEntityId

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryEntityId_NullOrWhitespace_ReturnsFalseWithError(string? value)
    {
        // Act
        var success = ToolArgs.TryEntityId(value, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("is required");
    }

    [Fact]
    public void TryEntityId_ValueHasSurroundingWhitespace_ReturnsTrueWithTrimmedId()
    {
        // Act
        var success = ToolArgs.TryEntityId("  entity_1  ", out var id, out var error);

        // Assert
        success.Should().BeTrue();
        id.Should().Be("entity_1");
        error.Should().BeNull();
    }

    #endregion
}
