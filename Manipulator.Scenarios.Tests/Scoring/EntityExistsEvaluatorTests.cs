using FluentAssertions;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class EntityExistsEvaluatorTests
{
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

    [Fact]
    public void Evaluate_Passes_WhenEveryRoleMatchesExpectedCount()
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
            .Build();
        var roles = new Dictionary<string, RoleSelector>
        {
            ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 2),
        };
        var spec = SpecWith(roles);
        var requirement = new EntityExistsRequirement("r1", Roles: ["stool"]);

        // Act
        var result = EntityExistsEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Fails_WhenActualCountDiffersFromExpected()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "stool"),
                e => e.WithComponent(new EntityName("Stool A"))
            )
            .Build();
        var roles = new Dictionary<string, RoleSelector>
        {
            ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 2),
        };
        var spec = SpecWith(roles);
        var requirement = new EntityExistsRequirement("r1", Roles: ["stool"]);

        // Act
        var result = EntityExistsEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("stool").And.Contain("expected 2").And.Contain("found 1");
    }

    [Fact]
    public void Evaluate_ReportsOnlyFailingRoles_WhenCheckingMultipleRoles()
    {
        // Arrange
        var scene = new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "stool"),
                e => e.WithComponent(new EntityName("Stool A"))
            )
            .Build();
        var roles = new Dictionary<string, RoleSelector>
        {
            ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 1),
            ["table"] = new RoleSelector(["table"], Geometry: null, Count: 1),
        };
        var spec = SpecWith(roles);
        var requirement = new EntityExistsRequirement("r1", Roles: ["stool", "table"]);

        // Act
        var result = EntityExistsEvaluator.Evaluate(requirement, spec, scene);

        // Assert
        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain("table").And.NotContain("stool");
    }
}
