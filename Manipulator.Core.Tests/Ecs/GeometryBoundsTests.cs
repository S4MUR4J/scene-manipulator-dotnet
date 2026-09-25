using FluentAssertions;
using Manipulator.Core.Ecs;

namespace Manipulator.Core.Tests.Ecs;

public class GeometryBoundsTests
{
    [Theory]
    [InlineData(GeometryType.Cube)]
    [InlineData(GeometryType.Sphere)]
    [InlineData(GeometryType.Cylinder)]
    [InlineData(GeometryType.Cone)]
    [InlineData(GeometryType.Capsule)]
    [InlineData(GeometryType.Plane)]
    [InlineData(GeometryType.Torus)]
    [InlineData(GeometryType.Hemisphere)]
    public void For_EveryGeometryType_ReturnsFiniteNonNegativeBounds(GeometryType geometry)
    {
        // Act
        var bounds = GeometryBounds.For(geometry);

        // Assert
        bounds.IsFinite().Should().BeTrue();
        bounds.X.Should().BeGreaterThanOrEqualTo(0f);
        bounds.Y.Should().BeGreaterThanOrEqualTo(0f);
        bounds.Z.Should().BeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public void For_Cube_IsUnitCube()
    {
        // Act
        var bounds = GeometryBounds.For(GeometryType.Cube);

        // Assert
        bounds.Should().Be(Vector3.One);
    }
}
