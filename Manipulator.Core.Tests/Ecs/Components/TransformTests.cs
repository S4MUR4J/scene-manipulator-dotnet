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
        var transform = new Transform(Position: position);

        // Assert
        transform.Position.Should().Be(position);
    }

    [Fact]
    public void Constructor_ExplicitRotation_StoresRotation()
    {
        // Arrange
        var rotation = new Vector3(45, 90, 0);

        // Act
        var transform = new Transform(Rotation: rotation);

        // Assert
        transform.Rotation.Should().Be(rotation);
    }

    [Fact]
    public void Constructor_ExplicitScale_StoresScale()
    {
        // Arrange
        var scale = new Vector3(2, 2, 2);

        // Act
        var transform = new Transform(Scale: scale);

        // Assert
        transform.Scale.Should().Be(scale);
    }

    [Fact]
    public void Constructor_ExplicitRotation_DoesNotAffectPosition()
    {
        // Arrange
        var rotation = new Vector3(45, 90, 0);

        // Act
        var transform = new Transform(Rotation: rotation);

        // Assert
        transform.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Constructor_ExplicitScale_DoesNotAffectPosition()
    {
        // Arrange
        var scale = new Vector3(2, 2, 2);

        // Act
        var transform = new Transform(Scale: scale);

        // Assert
        transform.Position.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Constructor_AllExplicit_StoresAllIndependently()
    {
        // Arrange
        var position = new Vector3(1, 0, 0);
        var rotation = new Vector3(0, 90, 0);
        var scale = new Vector3(2, 2, 2);

        // Act
        var transform = new Transform(position, rotation, scale);

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
        var rotation = new Vector3(0, 45, 0);
        var scale = new Vector3(3, 3, 3);
        var transform = new Transform(Rotation: rotation, Scale: scale);

        // Act
        var updated = transform with
        {
            Position = new Vector3(5, 0, 0),
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
