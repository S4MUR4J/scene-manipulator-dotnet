using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class ScaleOrderEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, RoleSelector> Roles = new Dictionary<
        string,
        RoleSelector
    >
    {
        ["small"] = new RoleSelector(["small"], Geometry: null, Count: 1),
        ["big"] = new RoleSelector(["big"], Geometry: null, Count: 1),
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

    private static Action<EntityBuilder> Cube(string name, Vector3 scale) =>
        e =>
            e.WithComponent(new EntityName(name))
                .WithComponent(new Transform { Position = Vector3.Zero, Scale = scale })
                .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null));

    private static Scene SceneWith(Vector3 smallScale, Vector3 bigScale) =>
        new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "small"), Cube("small", smallScale))
            .WithEntity(SceneBuilder.Id(2, "big"), Cube("big", bigScale))
            .Build();

    private static SpatialRelationRequirement Requirement() =>
        new SpatialRelationRequirement(
            "r1",
            SpatialRelation.ScaleOrder,
            Subjects: [],
            References: [],
            Params: new SpatialRelationParams(
                Order:
                [
                    ["small"],
                    ["big"],
                ],
                Metric: "aabb_volume"
            )
        );

    [Fact]
    public void Evaluate_Passes_WhenEarlierGroupIsSmaller()
    {
        // Arrange
        var scene = SceneWith(smallScale: Vector3.One, bigScale: new Vector3(2, 2, 2));

        // Act
        var result = SpatialRelationEvaluator.Evaluate(Requirement(), SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Passes_WhenVolumesAreEqual()
    {
        // Arrange
        var scene = SceneWith(smallScale: Vector3.One, bigScale: Vector3.One);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(Requirement(), SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Fails_WhenEarlierGroupIsLarger()
    {
        // Arrange
        var scene = SceneWith(smallScale: new Vector3(3, 3, 3), bigScale: Vector3.One);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(Requirement(), SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
    }
}
