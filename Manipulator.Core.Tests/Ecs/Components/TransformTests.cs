using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;

namespace Manipulator.Core.Tests.Ecs.Components;

public class TransformTests
{
    #region Defaults

    [Fact]
    public void Constructor_Default_PositionIsZero()
    {
        // Act
        var transform = new Transform();

        // Assert
        transform.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Constructor_Default_RotationIsZero()
    {
        // Act
        var transform = new Transform();

        // Assert
        transform.Rotation.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Constructor_Default_ScaleIsOne()
    {
        // Act
        var transform = new Transform();

        // Assert
        transform.Scale.Should().Be(Vector3.One);
    }

    #endregion

    #region Explicit Values

    [Fact]
    public void Constructor_ExplicitPosition_StoresPosition()
    {
        // Arrange
        var position = new Vector3(1, 2, 3);

        // Act
        var transform = new Transform { Position = position };

        // Assert
        transform.Position.Should().Be(position);
    }

    [Fact]
    public void Constructor_ExplicitRotation_StoresRotation()
    {
        // Arrange
        var rotation = new Vector3(45, 90, 0);

        // Act
        var transform = new Transform { Rotation = rotation };

        // Assert
        transform.Rotation.Should().Be(rotation);
    }

    [Fact]
    public void Constructor_ExplicitScale_StoresScale()
    {
        // Arrange
        var scale = Vector3.One * 2f;

        // Act
        var transform = new Transform { Scale = scale };

        // Assert
        transform.Scale.Should().Be(scale);
    }

    [Fact]
    public void Constructor_ExplicitRotation_DoesNotAffectPosition()
    {
        // Arrange
        var rotation = new Vector3(45, 90, 0);

        // Act
        var transform = new Transform { Rotation = rotation };

        // Assert
        transform.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Constructor_ExplicitScale_DoesNotAffectPosition()
    {
        // Arrange
        var scale = Vector3.One * 2f;

        // Act
        var transform = new Transform { Scale = scale };

        // Assert
        transform.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Constructor_AllExplicit_StoresAllIndependently()
    {
        // Arrange
        var position = Vector3.Right;
        var rotation = Vector3.Up * 90f;
        var scale = Vector3.One * 2f;

        // Act
        var transform = new Transform
        {
            Position = position,
            Rotation = rotation,
            Scale = scale,
        };

        // Assert
        transform.Position.Should().Be(position);
        transform.Rotation.Should().Be(rotation);
        transform.Scale.Should().Be(scale);
    }

    #endregion

    #region With Expression

    [Fact]
    public void With_ChangedPosition_PreservesOtherFields()
    {
        // Arrange
        var rotation = Vector3.Up * 45f;
        var scale = Vector3.One * 3f;
        var transform = new Transform { Rotation = rotation, Scale = scale };

        // Act
        var updated = transform with
        {
            Position = Vector3.Right * 5f,
        };

        // Assert
        updated.Rotation.Should().Be(rotation);
        updated.Scale.Should().Be(scale);
    }

    #endregion

    #region Type

    [Fact]
    public void Type_ReturnsTransform()
    {
        // Arrange
        var transform = new Transform();

        // Act
        var result = transform.Type;

        // Assert
        result.Should().Be(nameof(Transform));
    }

    #endregion
}
