using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Ecs;

public class WorldBoundsTests
{
    [Fact]
    public void Of_UnitCubeAtOrigin_SpansHalfExtentEachSide()
    {
        // Arrange
        var entity = new EntityBuilder("e1")
            .WithComponent(new Transform { Position = Vector3.Zero, Scale = Vector3.One })
            .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null))
            .Build();

        // Act
        var bounds = WorldBounds.Of(entity);

        // Assert
        bounds.Should().NotBeNull();
        bounds!.Value.Min.Should().Be(new Vector3(-0.5f, -0.5f, -0.5f));
        bounds.Value.Max.Should().Be(new Vector3(0.5f, 0.5f, 0.5f));
    }

    [Fact]
    public void Of_ScaledAndOffsetEntity_ScalesExtentsAndTranslatesCenter()
    {
        // Arrange
        var entity = new EntityBuilder("e1")
            .WithComponent(
                new Transform
                {
                    Position = new Vector3(1f, 2f, 3f),
                    Scale = new Vector3(2f, 1f, 1f),
                }
            )
            .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null))
            .Build();

        // Act
        var bounds = WorldBounds.Of(entity);

        // Assert
        bounds!.Value.Min.Should().Be(new Vector3(0f, 1.5f, 2.5f));
        bounds.Value.Max.Should().Be(new Vector3(2f, 2.5f, 3.5f));
    }

    [Fact]
    public void Of_MissingTransformOrMeshFilter_ReturnsNull()
    {
        // Arrange
        var entity = new EntityBuilder("e1").Build();

        // Act
        var bounds = WorldBounds.Of(entity);

        // Assert
        bounds.Should().BeNull();
    }
}
