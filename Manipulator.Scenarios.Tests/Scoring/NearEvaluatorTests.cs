using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class NearEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, RoleSelector> Roles = new Dictionary<
        string,
        RoleSelector
    >
    {
        ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 1),
        ["table"] = new RoleSelector(["table"], Geometry: null, Count: 1),
    };

    private static ScenarioSpec SpecWith(IReadOnlyDictionary<string, RoleSelector> roles) =>
        new ScenarioSpec(
            Id: "test",
            Category: "test",
            Prompt: "test",
            Variants: [],
            StartingScene: null,
            Roles: roles,
            Requirements: [],
            IterationLimit: 1,
            TimeoutSeconds: 1,
            Runs: new RunConfig(Main: 1, PerVariant: 0)
        );

    private static Action<EntityBuilder> Cube(string name, Vector3 position) =>
        e =>
            e.WithComponent(new EntityName(name))
                .WithComponent(new Transform { Position = position, Scale = Vector3.One })
                .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null));

    private static SpatialRelationRequirement RequirementWithMaxDistance(
        double maxDistanceM,
        string? plane = null
    ) =>
        new SpatialRelationRequirement(
            "r1",
            SpatialRelation.Near,
            Subjects: ["stool"],
            References: ["table"],
            Params: new SpatialRelationParams(MaxDistanceM: maxDistanceM, Plane: plane)
        );

    [Fact]
    public void Evaluate_Passes_WhenWithinMaxDistance()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "table"), Cube("table", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(0.5f, 0, 0)))
            .Build();
        var requirement = RequirementWithMaxDistance(1.0);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Passes_WhenDistanceIsExactlyAtMax()
    {
        // Arrange: distance exactly 1.0m, max 1.0m (boundary case)
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "table"), Cube("table", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(1.0f, 0, 0)))
            .Build();
        var requirement = RequirementWithMaxDistance(1.0);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Fails_WhenDistanceExceedsMax()
    {
        // Arrange: distance 1.5m, max 1.0m
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "table"), Cube("table", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(1.5f, 0, 0)))
            .Build();
        var requirement = RequirementWithMaxDistance(1.0);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_XzPlane_IgnoresVerticalDistance()
    {
        // Arrange: 2m apart on Y only - ignored on the xz plane, so effective distance is 0
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "table"), Cube("table", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(0, 2, 0)))
            .Build();
        var requirement = RequirementWithMaxDistance(1.0, plane: "xz");

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }
}
