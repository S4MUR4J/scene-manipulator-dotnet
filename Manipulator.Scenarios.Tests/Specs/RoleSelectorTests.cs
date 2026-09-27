using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Specs;

public class RoleSelectorTests
{
    private static Entity BuildEntity(string? name, string? geometry)
    {
        var componentsJson = string.Join(
            ",",
            new[]
            {
                name is null ? null : $"\"entity_name\":{{\"value\":\"{name}\"}}",
                geometry is null ? null : $"\"mesh_filter\":{{\"geometry\":\"{geometry}\"}}",
            }.Where(c => c is not null)
        );

        var json =
            $"{{\"version\":\"1.0\",\"scene_version\":1,\"entities\":[{{\"id\":\"e1\",\"components\":{{{componentsJson}}}}}]}}";

        return SceneSerializer.Deserialize(json).Scene.GetEntity("e1")!;
    }

    [Fact]
    public void Matches_NameContainsKeywordCaseInsensitive_ReturnsTrue()
    {
        var role = new RoleSelector(["sofa"], null, 1);
        var entity = BuildEntity("SOFA_1", null);

        role.Matches(entity).Should().BeTrue();
    }

    [Fact]
    public void Matches_NameDoesNotContainAnyKeyword_ReturnsFalse()
    {
        var role = new RoleSelector(["sofa"], null, 1);
        var entity = BuildEntity("CoffeeTable", null);

        role.Matches(entity).Should().BeFalse();
    }

    [Fact]
    public void Matches_NoEntityNameComponent_ReturnsFalse()
    {
        var role = new RoleSelector(["sofa"], null, 1);
        var entity = BuildEntity(null, "Cube");

        role.Matches(entity).Should().BeFalse();
    }

    [Fact]
    public void Matches_GeometryFilterSet_NarrowsAmbiguousName()
    {
        var floorRole = new RoleSelector(["floor"], [GeometryType.Plane], 1);
        var floorLamp = BuildEntity("FloorLamp", "Cylinder");
        var floor = BuildEntity("Floor", "Plane");

        floorRole.Matches(floorLamp).Should().BeFalse();
        floorRole.Matches(floor).Should().BeTrue();
    }

    [Fact]
    public void Matches_GeometryFilterNull_MatchesAnyGeometry()
    {
        var role = new RoleSelector(["wall"], null, 1);
        var entity = BuildEntity("BackWall", "Cube");

        role.Matches(entity).Should().BeTrue();
    }
}
