using FluentAssertions;
using FluentValidation.TestHelper;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Ecs.Components.Validators;

namespace Manipulator.Core.Tests.Ecs.Components;

public class MeshFilterTests
{
    private readonly MeshFilterValidator _validator = new MeshFilterValidator();

    #region Construction

    [Theory]
    [InlineData(GeometryType.Cube)]
    [InlineData(GeometryType.Sphere)]
    [InlineData(GeometryType.Cylinder)]
    [InlineData(GeometryType.Plane)]
    [InlineData(GeometryType.Torus)]
    [InlineData(GeometryType.Pyramid)]
    public void Constructor_AllGeometryTypes_ConstructSuccessfully(GeometryType geometry)
    {
        // Act
        var act = () => new MeshFilter(geometry, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullParameters_IsAllowed()
    {
        // Act
        var meshFilter = new MeshFilter(GeometryType.Cube, null);

        // Assert
        meshFilter.Parameters.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithParameters_StoresParameters()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["radius"] = 1.5f };

        // Act
        var meshFilter = new MeshFilter(GeometryType.Sphere, parameters);

        // Assert
        meshFilter.Parameters.Should().ContainKey("radius");
        meshFilter.Parameters!["radius"].Should().Be(1.5f);
    }

    #endregion

    #region Type

    [Fact]
    public void Type_ReturnsMeshFilter()
    {
        // Arrange
        var meshFilter = new MeshFilter(GeometryType.Cube, null);

        // Act
        var result = meshFilter.Type;

        // Assert
        result.Should().Be(nameof(MeshFilter));
    }

    #endregion

    #region Validation

    [Theory]
    [InlineData(GeometryType.Cube)]
    [InlineData(GeometryType.Sphere)]
    [InlineData(GeometryType.Cylinder)]
    [InlineData(GeometryType.Plane)]
    [InlineData(GeometryType.Torus)]
    [InlineData(GeometryType.Pyramid)]
    public void Validate_ValidGeometry_NoErrors(GeometryType geometry)
    {
        // Arrange
        var meshFilter = new MeshFilter(geometry, null);

        // Act
        var result = _validator.TestValidate(meshFilter);

        // Assert
        result.ShouldNotHaveValidationErrorFor(mf => mf.Geometry);
    }

    [Fact]
    public void Validate_InvalidGeometry_HasError()
    {
        // Arrange
        var meshFilter = new MeshFilter((GeometryType)99, null);

        // Act
        var result = _validator.TestValidate(meshFilter);

        // Assert
        result.ShouldHaveValidationErrorFor(mf => mf.Geometry);
    }

    #endregion
}