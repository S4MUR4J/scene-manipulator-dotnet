using FluentAssertions;
using Manipulator.Core.Ecs;

namespace Manipulator.Core.Tests.Ecs;

public class Vector3Tests
{
    #region Scalar Multiplication

    [Fact]
    public void Multiply_VectorByScalar_ScalesAllComponents()
    {
        // Act
        var result = Vector3.Right * 3f;

        // Assert
        result.Should().Be(new Vector3(3, 0, 0));
    }

    [Fact]
    public void Multiply_ScalarByVector_ScalesAllComponents()
    {
        // Act
        var result = 3f * Vector3.Right;

        // Assert
        result.Should().Be(new Vector3(3, 0, 0));
    }

    [Fact]
    public void Multiply_VectorByScalar_OnePreservesDirection()
    {
        // Act
        var result = Vector3.Up * 1f;

        // Assert
        result.Should().Be(Vector3.Up);
    }

    [Fact]
    public void Multiply_VectorByZero_ReturnsZero()
    {
        // Act
        var result = Vector3.Forward * 0f;

        // Assert
        result.Should().Be(Vector3.Zero);
    }

    #endregion
}
