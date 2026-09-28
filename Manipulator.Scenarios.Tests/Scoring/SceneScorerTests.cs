using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Tests.Helpers;
using Manipulator.Scenarios.Loading;
using Manipulator.Scenarios.Scoring;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Scoring;

public class SceneScorerTests
{
    [Fact]
    public void Score_MixesEveryRequirementType_AndAggregatesCoverage()
    {
        // Arrange: sofa exists and is red (both pass), but a stool count is wrong (fails)
        var roles = new Dictionary<string, RoleSelector>
        {
            ["sofa"] = new RoleSelector(["sofa"], Geometry: null, Count: 1),
            ["stool"] = new RoleSelector(["stool"], Geometry: null, Count: 2),
        };
        var spec = new ScenarioSpec(
            Id: "test",
            Category: "test",
            Prompt: "test",
            Variants: [],
            StartingScene: null,
            Roles: roles,
            Requirements:
            [
                new EntityExistsRequirement("R1-sofa-exists", Roles: ["sofa"]),
                new EntityExistsRequirement("R2-stools-exist", Roles: ["stool"]),
                new ComponentValueRequirement(
                    "R3-sofa-is-red",
                    Role: "sofa",
                    Component: "MeshRenderer",
                    Field: "color",
                    Constraint: new ComponentValueConstraint(ExpectedValue: "#ff0000")
                ),
            ],
            IterationLimit: 1,
            TimeoutSeconds: 1,
            Runs: new RunConfig(Main: 1, PerVariant: 0)
        );
        var scene = new SceneBuilder()
            .WithEntity(
                SceneBuilder.Id(1, "sofa"),
                e =>
                    e.WithComponent(new EntityName("Sofa"))
                        .WithComponent(new MeshRenderer(Color: "#ff0000"))
            )
            .WithEntity(
                SceneBuilder.Id(2, "stool"),
                e => e.WithComponent(new EntityName("Stool A"))
            )
            .Build();

        // Act
        var result = SceneScorer.Score(spec, scene);

        // Assert: R1 and R3 pass, R2 fails (only 1 of 2 stools) -> 2/3 coverage, not success
        result.Requirements.Should().HaveCount(3);
        result
            .Requirements.Single(r => r.RequirementId == "R1-sofa-exists")
            .Passed.Should()
            .BeTrue();
        result
            .Requirements.Single(r => r.RequirementId == "R2-stools-exist")
            .Passed.Should()
            .BeFalse();
        result
            .Requirements.Single(r => r.RequirementId == "R3-sofa-is-red")
            .Passed.Should()
            .BeTrue();
        result.Coverage.Should().BeApproximately(2.0 / 3.0, 1e-9);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Score_Scenario1FixtureAgainstAConformingScene_IsFullySuccessful()
    {
        // Arrange: a scene satisfying every requirement in the real scenario 1 spec - proves the
        // scorer, not just individual evaluators, handles a real spec end to end.
        var spec = ScenarioSpecLoader.LoadFile("Fixtures/s1-new-gen-livingroom.json");
        var furnitureScale = new Vector3(0.3f, 0.5f, 0.3f);

        var scene = new SceneBuilder()
            .WithEntity(
                "floor_1",
                e =>
                    e.WithComponent(new EntityName("Floor"))
                        .WithComponent(
                            new Transform
                            {
                                Position = new Vector3(0, 0, 0),
                                Scale = new Vector3(20, 1, 20),
                            }
                        )
                        .WithComponent(new MeshFilter(GeometryType.Plane, Parameters: null))
            )
            .WithEntity(
                "wall_1",
                e =>
                    e.WithComponent(new EntityName("Back Wall"))
                        .WithComponent(new Transform { Position = new Vector3(0, 1, 6) })
                        .WithComponent(new MeshFilter(GeometryType.Plane, Parameters: null))
            )
            .WithEntity(
                "sofa_1",
                e =>
                    e.WithComponent(new EntityName("Sofa"))
                        .WithComponent(
                            new Transform
                            {
                                Position = new Vector3(0, 0.25f, 0),
                                Scale = furnitureScale,
                            }
                        )
                        .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null))
            )
            .WithEntity(
                "coffee_table_1",
                e =>
                    e.WithComponent(new EntityName("Coffee Table"))
                        .WithComponent(
                            new Transform
                            {
                                Position = new Vector3(0, 0.25f, 3),
                                Scale = furnitureScale,
                            }
                        )
                        .WithComponent(new MeshFilter(GeometryType.Cube, Parameters: null))
            )
            .WithEntity(
                "stool_1",
                e =>
                    e.WithComponent(new EntityName("Stool 1"))
                        .WithComponent(
                            new Transform
                            {
                                Position = new Vector3(0.75f, 0.25f, 3),
                                Scale = furnitureScale,
                            }
                        )
                        .WithComponent(new MeshFilter(GeometryType.Cylinder, Parameters: null))
            )
            .WithEntity(
                "stool_2",
                e =>
                    e.WithComponent(new EntityName("Stool 2"))
                        .WithComponent(
                            new Transform
                            {
                                Position = new Vector3(-0.75f, 0.25f, 3),
                                Scale = furnitureScale,
                            }
                        )
                        .WithComponent(new MeshFilter(GeometryType.Cylinder, Parameters: null))
            )
            .WithEntity(
                "lamp_1",
                e =>
                    e.WithComponent(new EntityName("Lamp"))
                        .WithComponent(
                            new Transform
                            {
                                Position = new Vector3(0.75f, 0.25f, 0),
                                Scale = furnitureScale,
                            }
                        )
                        .WithComponent(new MeshFilter(GeometryType.Cylinder, Parameters: null))
            )
            .Build();

        // Act
        var result = SceneScorer.Score(spec, scene);

        // Assert
        result
            .Requirements.Where(r => !r.Passed)
            .Should()
            .BeEmpty(
                because: string.Join(
                    "; ",
                    result
                        .Requirements.Where(r => !r.Passed)
                        .Select(r => $"{r.RequirementId}: {r.Reason}")
                )
            );
        result.Coverage.Should().Be(1.0);
        result.Success.Should().BeTrue();
    }
}
