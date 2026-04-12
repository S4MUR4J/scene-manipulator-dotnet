using FluentAssertions;
using FluentValidation.TestHelper;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Ecs.Components.Validators;

namespace Manipulator.Core.Tests.Ecs.Components;

public class MeshRendererTests
{
    private readonly MeshRendererValidator _validator = new MeshRendererValidator();

    #region Defaults

    [Fact]
    public void Constructor_Default_ColorIsWhite()
    {
        // Act
        var meshRenderer = new MeshRenderer();

        // Assert
        meshRenderer.Color.Should().Be("#ffffff");
    }

    [Fact]
    public void Constructor_Default_OpacityIsOne()
    {
        // Act
        var meshRenderer = new MeshRenderer();

        // Assert
        meshRenderer.Opacity.Should().Be(1.0f);
    }

    [Fact]
    public void Constructor_Default_MetalnessIsZero()
    {
        // Act
        var meshRenderer = new MeshRenderer();

        // Assert
        meshRenderer.Metalness.Should().Be(0.0f);
    }

    [Fact]
    public void Constructor_Default_RoughnessIsHalf()
    {
        // Act
        var meshRenderer = new MeshRenderer();

        // Assert
        meshRenderer.Roughness.Should().Be(0.5f);
    }

    #endregion

    #region Type

    [Fact]
    public void Type_ReturnsMeshRenderer()
    {
        // Arrange
        var meshRenderer = new MeshRenderer();

        // Act
        var result = meshRenderer.Type;

        // Assert
        result.Should().Be(nameof(MeshRenderer));
    }

    #endregion

    #region Validation — Opacity

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Validate_OpacityInRange_NoError(float opacity)
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Opacity: opacity);

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldNotHaveValidationErrorFor(mr => mr.Opacity);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_OpacityOutOfRange_HasError(float opacity)
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Opacity: opacity);

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldHaveValidationErrorFor(mr => mr.Opacity);
    }

    #endregion

    #region Validation — Metalness

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Validate_MetalnessInRange_NoError(float metalness)
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Metalness: metalness);

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldNotHaveValidationErrorFor(mr => mr.Metalness);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_MetalnessOutOfRange_HasError(float metalness)
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Metalness: metalness);

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldHaveValidationErrorFor(mr => mr.Metalness);
    }

    #endregion

    #region Validation — Roughness

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Validate_RoughnessInRange_NoError(float roughness)
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Roughness: roughness);

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldNotHaveValidationErrorFor(mr => mr.Roughness);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_RoughnessOutOfRange_HasError(float roughness)
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Roughness: roughness);

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldHaveValidationErrorFor(mr => mr.Roughness);
    }

    #endregion

    #region Validation — Color

    [Fact]
    public void Validate_NonEmptyColor_NoError()
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Color: "#ff0000");

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldNotHaveValidationErrorFor(mr => mr.Color);
    }

    [Fact]
    public void Validate_EmptyColor_HasError()
    {
        // Arrange
        var meshRenderer = new MeshRenderer(Color: "");

        // Act
        var result = _validator.TestValidate(meshRenderer);

        // Assert
        result.ShouldHaveValidationErrorFor(mr => mr.Color);
    }

    #endregion
}
