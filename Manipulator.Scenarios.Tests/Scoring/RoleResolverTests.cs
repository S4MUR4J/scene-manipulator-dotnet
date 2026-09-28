using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class RoleResolverTests
{
    [Fact]
    public void Resolve_ReturnsEveryMatchingEntity_NotJustOne()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "stool"),
                e => e.WithComponent(new EntityName("Stool A"))
            )
            .WithEntity(
                SceneBuilder.Id(2, "stool"),
                e => e.WithComponent(new EntityName("Stool B"))
            )
            .WithEntity(
                SceneBuilder.Id(3, "table"),
                e => e.WithComponent(new EntityName("Dining Table"))
            )
            .Build();
        var roles = new Dictionary<string, RoleSelector>
        {
            ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 2),
        };

        // Act
        var matches = RoleResolver.Resolve(scene, roles, "stool");

        // Assert
        matches
            .Select(e => e.Id)
            .Should()
            .BeEquivalentTo(SceneBuilder.Id(1, "stool"), SceneBuilder.Id(2, "stool"));
    }

    [Fact]
    public void Resolve_NarrowsByGeometry_WhenSelectorDeclaresIt()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "seat"),
                e =>
                    e.WithComponent(new EntityName("Seat"))
                        .WithComponent(new MeshFilter(GeometryType.Cylinder, Parameters: null))
            )
            .WithEntity(
                SceneBuilder.Id(2, "seat"),
                e =>
                    e.WithComponent(new EntityName("Seat"))
                        .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null))
            )
            .Build();
        var roles = new Dictionary<string, RoleSelector>
        {
            ["stool"] = new RoleSelector(["seat"], Geometry: [GeometryType.Cylinder], Count: 1),
        };

        // Act
        var matches = RoleResolver.Resolve(scene, roles, "stool");

        // Assert
        matches.Select(e => e.Id).Should().Equal(SceneBuilder.Id(1, "seat"));
    }

    [Fact]
    public void Resolve_ReturnsEmpty_WhenNothingMatches()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "table"),
                e => e.WithComponent(new EntityName("Dining Table"))
            )
            .Build();
        var roles = new Dictionary<string, RoleSelector>
        {
            ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 1),
        };

        // Act
        var matches = RoleResolver.Resolve(scene, roles, "stool");

        // Assert
        matches.Should().BeEmpty();
    }
}
