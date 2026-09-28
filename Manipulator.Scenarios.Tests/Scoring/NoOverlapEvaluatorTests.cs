using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class NoOverlapEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, RoleSelector> Roles = new Dictionary<
        string,
        RoleSelector
    >
    {
        ["item"] = new RoleSelector(["item"], Geometry: null, Count: 2),
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

    private static Scene SceneWithTwoItems(Vector3 posA, Vector3 posB) =>
        new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "item"), Cube("item", posA))
            .WithEntity(SceneBuilder.Id(2, "item"), Cube("item", posB))
            .Build();

    private static SpatialRelationRequirement RequirementWithMinSeparation(double minSeparationM) =>
        new SpatialRelationRequirement(
            "r1",
            SpatialRelation.NoOverlap,
            Subjects: ["item"],
            References: [],
            Params: new SpatialRelationParams(MinSeparationM: minSeparationM)
        );

    [Fact]
    public void Evaluate_Fails_WhenBoxesOverlap()
    {
        // Arrange: unit cubes centered 0.5m apart - well within overlap
        var scene = SceneWithTwoItems(new Vector3(0, 0, 0), new Vector3(0.5f, 0, 0));
        var requirement = RequirementWithMinSeparation(0.4);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Passes_WhenSeparationIsExactlyTheMinimum()
    {
        // Arrange: unit cubes (half-extent 0.5) with a gap of exactly 0.5m -> centers 1.5m apart.
        // 0.5 and 1.5 are exact in binary floating point, unlike 0.4/1.4, so this isn't sensitive
        // to float rounding.
        var scene = SceneWithTwoItems(new Vector3(0, 0, 0), new Vector3(1.5f, 0, 0));
        var requirement = RequirementWithMinSeparation(0.5);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Fails_WhenSeparationIsBelowMinimum()
    {
        // Arrange: gap of 0.1m (centers 1.1m apart), min separation 0.4m
        var scene = SceneWithTwoItems(new Vector3(0, 0, 0), new Vector3(1.1f, 0, 0));
        var requirement = RequirementWithMinSeparation(0.4);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Passes_WhenWellSeparated()
    {
        // Arrange
        var scene = SceneWithTwoItems(new Vector3(0, 0, 0), new Vector3(5, 0, 0));
        var requirement = RequirementWithMinSeparation(0.4);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }
}
