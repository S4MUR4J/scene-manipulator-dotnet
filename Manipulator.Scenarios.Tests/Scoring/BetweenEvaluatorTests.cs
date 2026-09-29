using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class BetweenEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, RoleSelector> Roles = new Dictionary<
        string,
        RoleSelector
    >
    {
        ["table"] = new RoleSelector(["table"], Geometry: null, Count: 1),
        ["sofa"] = new RoleSelector(["sofa"], Geometry: null, Count: 1),
        ["wall"] = new RoleSelector(["wall"], Geometry: null, Count: 1),
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

    private static Scene SceneWith(Vector3 sofaPos, Vector3 tablePos, Vector3 wallPos) =>
        new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "sofa"), Cube("sofa", sofaPos))
            .WithEntity(SceneBuilder.Id(2, "table"), Cube("table", tablePos))
            .WithEntity(SceneBuilder.Id(3, "wall"), Cube("wall", wallPos))
            .Build();

    private static SpatialRelationRequirement RequirementWithMargin(double marginM) =>
        new SpatialRelationRequirement(
            "r1",
            SpatialRelation.Between,
            Subjects: ["table"],
            References: ["sofa", "wall"],
            Params: new SpatialRelationParams(MarginM: marginM)
        );

    [Fact]
    public void Evaluate_Passes_WhenSubjectIsBetweenReferences()
    {
        // Arrange
        var scene = SceneWith(
            sofaPos: new Vector3(0, 0, 0),
            tablePos: new Vector3(0, 0, 2),
            wallPos: new Vector3(0, 0, 4)
        );
        var requirement = RequirementWithMargin(0.3);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Passes_WhenBeyondReferenceByExactlyTheMargin()
    {
        // Arrange: 0.25 is exact in binary floating point, unlike 0.3, so the boundary isn't
        // float-rounding sensitive
        var scene = SceneWith(
            sofaPos: new Vector3(0, 0, 0),
            tablePos: new Vector3(0, 0, 4.25f),
            wallPos: new Vector3(0, 0, 4)
        );
        var requirement = RequirementWithMargin(0.25);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Fails_WhenBeyondReferenceByMoreThanMargin()
    {
        // Arrange
        var scene = SceneWith(
            sofaPos: new Vector3(0, 0, 0),
            tablePos: new Vector3(0, 0, 5.5f),
            wallPos: new Vector3(0, 0, 4)
        );
        var requirement = RequirementWithMargin(0.3);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Fails_WhenBeforeFirstReferenceByMoreThanMargin()
    {
        // Arrange
        var scene = SceneWith(
            sofaPos: new Vector3(0, 0, 0),
            tablePos: new Vector3(0, 0, -1.5f),
            wallPos: new Vector3(0, 0, 4)
        );
        var requirement = RequirementWithMargin(0.3);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Fails_WhenAReferenceRoleHasNoEntity()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(SceneBuilder.Id(1, "sofa"), Cube("sofa", Vector3.Zero))
            .WithEntity(SceneBuilder.Id(2, "table"), Cube("table", new Vector3(0, 0, 2)))
            .Build();
        var requirement = RequirementWithMargin(0.3);

        // Act
        var result = SpatialRelationEvaluator.Evaluate(requirement, SpecWith(Roles), scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("found 1");
    }
}
