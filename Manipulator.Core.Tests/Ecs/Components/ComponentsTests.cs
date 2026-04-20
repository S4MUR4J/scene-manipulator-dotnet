using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using static Manipulator.Core.Tests.Helpers.TestUtils;

namespace Manipulator.Core.Tests.Ecs.Components;

public class ComponentsTests
{
    #region Type Property

    public static IEnumerable<object[]> AllComponentsWithExpectedType =>
        [
            [new Transform(), nameof(Transform)],
            [new EntityName(), nameof(EntityName)],
            [new MeshFilter(GeometryType.Cube, null), nameof(MeshFilter)],
            [new MeshRenderer(), nameof(MeshRenderer)],
        ];

    [Theory]
    [MemberData(nameof(AllComponentsWithExpectedType))]
    public void Type_ReturnsComponentTypeName(IComponent component, string expectedType)
    {
        // Act
        var result = component.Type;

        // Assert
        result.Should().Be(expectedType);
    }

    #endregion

    #region Record Equality

    [Fact]
    public void Transform_EqualInstances_AreEqual()
    {
        // Arrange
        var position = new Vector3(1, 2, 3);
        var firstTransform = new Transform { Position = position };
        var secondTransform = new Transform { Position = position };

        // Assert
        firstTransform.Should().Be(secondTransform);
    }

    [Fact]
    public void Transform_DifferentInstances_AreNotEqual()
    {
        // Arrange
        var firstTransform = new Transform { Position = Vector3.Right };
        var secondTransform = new Transform { Position = Vector3.Up };

        // Assert
        firstTransform.Should().NotBe(secondTransform);
    }

    [Fact]
    public void EntityName_EqualInstances_AreEqual()
    {
        // Arrange
        var firstEntityName = new EntityName(Tag("name", 1));
        var secondEntityName = new EntityName(Tag("name", 1));

        // Assert
        firstEntityName.Should().Be(secondEntityName);
    }

    [Fact]
    public void MeshFilter_EqualInstances_AreEqual()
    {
        // Arrange
        var firstMeshFilter = new MeshFilter(GeometryType.Sphere, null);
        var secondMeshFilter = new MeshFilter(GeometryType.Sphere, null);

        // Assert
        firstMeshFilter.Should().Be(secondMeshFilter);
    }

    [Fact]
    public void MeshRenderer_EqualInstances_AreEqual()
    {
        // Arrange
        var firstMeshRenderer = new MeshRenderer(Opacity: 0.5f);
        var secondMeshRenderer = new MeshRenderer(Opacity: 0.5f);

        // Assert
        firstMeshRenderer.Should().Be(secondMeshRenderer);
    }

    [Fact]
    public void MeshRenderer_DifferentInstances_AreNotEqual()
    {
        // Arrange
        var firstMeshRenderer = new MeshRenderer(Opacity: 0.2f);
        var secondMeshRenderer = new MeshRenderer(Opacity: 0.8f);

        // Assert
        firstMeshRenderer.Should().NotBe(secondMeshRenderer);
    }

    #endregion
}
