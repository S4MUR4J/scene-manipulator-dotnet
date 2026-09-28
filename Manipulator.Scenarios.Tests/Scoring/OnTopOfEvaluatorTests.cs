using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class OnTopOfEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, RoleSelector> Roles = new Dictionary<
        string,
        RoleSelector
    >
    {
        ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 1),
        ["floor"] = new RoleSelector(["floor"], Geometry: null, Count: 1),
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

    private static Action<EntityBuilder> Cube(
        string name,
        Vector3 position,
        Vector3? scale = null
    ) =>
        e =>
            e.WithComponent(new EntityName(name))
                .WithComponent(new Transform { Position = position, Scale = scale ?? Vector3.One })
                .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null));

    private static SpatialRelationRequirement RequirementWithTolerance(double toleranceM) =>
        new SpatialRelationRequirement(
            "r1",
            SpatialRelation.OnTopOf,
            Subjects: ["stool"],
            References: ["floor"],
            Params: new SpatialRelationParams(ToleranceM: toleranceM)
        );

    [Fact]
    public void Evaluate_Passes_WhenSubjectRestsExactlyOnReference()
    {
        // Arrange: unit-cube floor top face at y=0.5, stool bottom face at y=0.5 (resting flush)
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "floor"), Cube("floor", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(0, 1, 0)))
            .Build();
        var requirement = RequirementWithTolerance(0.05);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Passes_WhenGapIsExactlyAtTolerance()
    {
        // Arrange: gap of exactly 0.05m, tolerance 0.05m (boundary case)
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "floor"), Cube("floor", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(0, 1.05f, 0)))
            .Build();
        var requirement = RequirementWithTolerance(0.05);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Fails_WhenGapExceedsTolerance()
    {
        // Arrange: gap of 0.2m, tolerance 0.05m
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "floor"), Cube("floor", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(0, 1.2f, 0)))
            .Build();
        var requirement = RequirementWithTolerance(0.05);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Fails_WhenNoHorizontalOverlap()
    {
        // Arrange: vertically aligned but shifted far away on X, no XZ overlap with the floor
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "floor"), Cube("floor", new Vector3(0, 0, 0)))
            .WithEntity(SceneBuilder.Id(2, "stool"), Cube("stool", new Vector3(10, 1, 0)))
            .Build();
        var requirement = RequirementWithTolerance(0.05);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("no horizontal overlap");
    }
}
