using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class ComponentValueEvaluatorTests
{
    private static readonly IReadOnlyDictionary<string, RoleSelector> Roles = new Dictionary<
        string,
        RoleSelector
    >
    {
        ["sofa"] = new RoleSelector(["sofa"], Geometry: null, Count: 1),
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

    private static Scene SceneWithSofa(Action<EntityBuilder> configure) =>
        new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "sofa"),
                e =>
                {
                    e.WithComponent(new EntityName("Sofa"));
                    configure(e);
                }
            )
            .Build();

    [Fact]
    public void Evaluate_ExpectedValue_Passes_OnExactColorMatch()
    {
        // Arrange
        var scene = SceneWithSofa(e => e.WithComponent(new MeshRenderer(Color: "#ff0000")));
        var spec = SpecWith(Roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "sofa",
            Component: "MeshRenderer",
            Field: "color",
            Constraint: new ComponentValueConstraint(ExpectedValue: "#FF0000")
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ExpectedValue_Fails_OnColorMismatch()
    {
        // Arrange
        var scene = SceneWithSofa(e => e.WithComponent(new MeshRenderer(Color: "#00ff00")));
        var spec = SpecWith(Roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "sofa",
            Component: "MeshRenderer",
            Field: "color",
            Constraint: new ComponentValueConstraint(ExpectedValue: "#ff0000")
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("#ff0000").And.Contain("#00ff00");
    }

    [Fact]
    public void Evaluate_HueRange_HandlesWraparoundAcrossZeroDegrees()
    {
        // Arrange: red (hue 0) inside a [350, 10] wraparound range
        var scene = SceneWithSofa(e => e.WithComponent(new MeshRenderer(Color: "#ff0000")));
        var spec = SpecWith(Roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "sofa",
            Component: "MeshRenderer",
            Field: "color",
            Constraint: new ComponentValueConstraint(HueMinDeg: 350, HueMaxDeg: 10)
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_HueRange_Fails_WhenOutsideRange()
    {
        // Arrange: green (hue 120) outside [0, 30]
        var scene = SceneWithSofa(e => e.WithComponent(new MeshRenderer(Color: "#00ff00")));
        var spec = SpecWith(Roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "sofa",
            Component: "MeshRenderer",
            Field: "color",
            Constraint: new ComponentValueConstraint(HueMinDeg: 0, HueMaxDeg: 30)
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_MinMax_ChecksTransformScaleAxis()
    {
        // Arrange
        var scene = SceneWithSofa(e =>
            e.WithComponent(new Transform { Scale = new Vector3(2.5f, 1f, 1f) })
        );
        var spec = SpecWith(Roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "sofa",
            Component: "Transform",
            Field: "scale.x",
            Constraint: new ComponentValueConstraint(Min: 2.0, Max: 3.0)
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_MinMax_Fails_WhenBelowMinimum()
    {
        // Arrange
        var scene = SceneWithSofa(e => e.WithComponent(new MeshRenderer(Opacity: 0.1f)));
        var spec = SpecWith(Roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "sofa",
            Component: "MeshRenderer",
            Field: "opacity",
            Constraint: new ComponentValueConstraint(Min: 0.5)
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("below minimum");
    }

    [Fact]
    public void Evaluate_Fails_WhenRoleHasNoMatches()
    {
        // Arrange
        var scene = new Scene();
        var spec = SpecWith(Roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "sofa",
            Component: "MeshRenderer",
            Field: "color",
            Constraint: new ComponentValueConstraint(ExpectedValue: "#ff0000")
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("no matching entities");
    }

    [Fact]
    public void Evaluate_ChecksEveryMatchingEntity_NotJustOne()
    {
        // Arrange
        var roles = new Dictionary<string, RoleSelector>
        {
            ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 2),
        };
        var scene = new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "stool"),
                e =>
                    e.WithComponent(new EntityName("Stool A"))
                        .WithComponent(new MeshRenderer(Color: "#ff0000"))
            )
            .WithEntity(
                SceneBuilder.Id(2, "stool"),
                e =>
                    e.WithComponent(new EntityName("Stool B"))
                        .WithComponent(new MeshRenderer(Color: "#00ff00"))
            )
            .Build();
        var spec = SpecWith(roles);
        var requirement = new ComponentValueRequirement(
            "r1",
            Role: "stool",
            Component: "MeshRenderer",
            Field: "color",
            Constraint: new ComponentValueConstraint(ExpectedValue: "#ff0000")
        );

        // Act
        var result = ComponentValueEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(SceneBuilder.Id(2, "stool"));
    }
}
