using FluentAssertions;
using Manipulator.Scenarios.Loading;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Tests.Loading;

public class ScenarioSpecLoaderTests
{
    private const string RequirementsPlaceholder =
        "\"requirements\":[{\"id\":\"R1\",\"type\":\"entity_exists\",\"roles\":[\"sofa\"]}]";

    private const string ValidMinimalSpec =
        "{"
        + "\"id\":\"S-test\","
        + "\"category\":\"new_scene_generation\","
        + "\"prompt\":\"Do the thing.\","
        + "\"roles\":{\"sofa\":{\"name_contains\":[\"sofa\"],\"geometry\":[\"Cube\"],\"count\":1}},"
        + RequirementsPlaceholder
        + ","
        + "\"iteration_limit\":1,"
        + "\"timeout_s\":60,"
        + "\"runs\":{\"main\":10,\"per_variant\":5}"
        + "}";

    #region Scenario 1 fixture

    [Fact]
    public void LoadFile_Scenario1Fixture_ProducesExpectedSpec()
    {
        var spec = ScenarioSpecLoader.LoadFile("Fixtures/s1-new-gen-livingroom.json");

        spec.Id.Should().Be("S1-new-gen-livingroom");
        spec.Category.Should().Be("new_scene_generation");
        spec.Variants.Should().HaveCount(2);
        spec.StartingScene.Should().BeNull();
        spec.Roles.Should().HaveCount(6);
        spec.Roles["stool"].Count.Should().Be(2);
        spec.Requirements.Should().HaveCount(7);
        spec.IterationLimit.Should().Be(1);
        spec.TimeoutSeconds.Should().Be(120);
        spec.Runs.Should().Be(new RunConfig(10, 5));
    }

    [Fact]
    public void LoadFile_Scenario1Fixture_EncodesEveryRelationFromThePrompt()
    {
        var spec = ScenarioSpecLoader.LoadFile("Fixtures/s1-new-gen-livingroom.json");

        var relations = spec
            .Requirements.OfType<SpatialRelationRequirement>()
            .Select(r => r.Relation)
            .ToList();

        relations
            .Should()
            .BeEquivalentTo([
                SpatialRelation.OnTopOf,
                SpatialRelation.Between,
                SpatialRelation.Near,
                SpatialRelation.Near,
                SpatialRelation.NoOverlap,
                SpatialRelation.ScaleOrder,
            ]);
    }

    #endregion

    #region Structural validation

    [Fact]
    public void Load_ValidMinimalSpec_Succeeds()
    {
        var spec = ScenarioSpecLoader.Load(ValidMinimalSpec);

        spec.Id.Should().Be("S-test");
    }

    [Fact]
    public void Load_MissingId_Throws()
    {
        var json = ValidMinimalSpec.Replace("\"id\":\"S-test\",", "");

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*'id'*");
    }

    [Fact]
    public void Load_InvalidJson_Throws()
    {
        var act = () => ScenarioSpecLoader.Load("not json");

        act.Should().Throw<ScenarioSpecException>().WithMessage("Invalid JSON:*");
    }

    [Fact]
    public void Load_NoRoles_Throws()
    {
        var json = ValidMinimalSpec.Replace(
            "\"roles\":{\"sofa\":{\"name_contains\":[\"sofa\"],\"geometry\":[\"Cube\"],\"count\":1}},",
            "\"roles\":{},"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*roles*");
    }

    [Fact]
    public void Load_RequirementReferencesUndeclaredRole_Throws()
    {
        var json = ValidMinimalSpec.Replace("\"roles\":[\"sofa\"]", "\"roles\":[\"lamp\"]");

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*undeclared role 'lamp'*");
    }

    [Fact]
    public void Load_UnknownRequirementType_Throws()
    {
        var json = ValidMinimalSpec.Replace("\"type\":\"entity_exists\"", "\"type\":\"bogus\"");

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*unknown type 'bogus'*");
    }

    [Fact]
    public void Load_DuplicateRequirementId_Throws()
    {
        var json = ValidMinimalSpec.Replace(
            RequirementsPlaceholder,
            "\"requirements\":[{\"id\":\"R1\",\"type\":\"entity_exists\",\"roles\":[\"sofa\"]},"
                + "{\"id\":\"R1\",\"type\":\"entity_exists\",\"roles\":[\"sofa\"]}]"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*Duplicate requirement id*");
    }

    [Fact]
    public void Load_UnknownGeometryInRole_Throws()
    {
        var json = ValidMinimalSpec.Replace("[\"Cube\"]", "[\"Blob\"]");

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*unknown geometry 'Blob'*");
    }

    #endregion

    #region Spatial relation validation

    private static string WithSpatialRequirement(string requirementJson) =>
        ValidMinimalSpec.Replace(RequirementsPlaceholder, $"\"requirements\":[{requirementJson}]");

    [Fact]
    public void Load_BetweenWithWrongReferenceCount_Throws()
    {
        var json = WithSpatialRequirement(
            "{\"id\":\"R2\",\"type\":\"spatial_relation\",\"relation\":\"between\","
                + "\"subjects\":[\"sofa\"],\"references\":[\"sofa\"],\"params\":{\"margin_m\":0.3}}"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*between*exactly 2 roles*");
    }

    [Fact]
    public void Load_NearWithoutMaxDistance_Throws()
    {
        var json = WithSpatialRequirement(
            "{\"id\":\"R2\",\"type\":\"spatial_relation\",\"relation\":\"near\","
                + "\"subjects\":[\"sofa\"],\"references\":[\"sofa\"],\"params\":{}}"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*max_distance_m*");
    }

    [Fact]
    public void Load_NoOverlapWithFewerThanTwoSubjects_Throws()
    {
        var json = WithSpatialRequirement(
            "{\"id\":\"R2\",\"type\":\"spatial_relation\",\"relation\":\"no_overlap\","
                + "\"subjects\":[\"sofa\"],\"params\":{\"min_separation_m\":0.4}}"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*no_overlap*at least 2 roles*");
    }

    [Fact]
    public void Load_ScaleOrderWithSingleGroup_Throws()
    {
        var json = WithSpatialRequirement(
            "{\"id\":\"R2\",\"type\":\"spatial_relation\",\"relation\":\"scale_order\","
                + "\"params\":{\"order\":[[\"sofa\"]],\"metric\":\"aabb_volume\"}}"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should()
            .Throw<ScenarioSpecException>()
            .WithMessage("*scale_order*at least 2 role groups*");
    }

    [Fact]
    public void Load_ScaleOrderReferencingUndeclaredRole_Throws()
    {
        var json = WithSpatialRequirement(
            "{\"id\":\"R2\",\"type\":\"spatial_relation\",\"relation\":\"scale_order\","
                + "\"params\":{\"order\":[[\"sofa\"],[\"lamp\"]],\"metric\":\"aabb_volume\"}}"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*undeclared role 'lamp'*");
    }

    #endregion

    #region Starting scene

    [Fact]
    public void Load_StartingSceneProvided_DeserializesIntoScene()
    {
        var json = ValidMinimalSpec.Replace(
            "\"category\":\"new_scene_generation\",",
            "\"category\":\"new_scene_generation\","
                + "\"starting_scene\":{\"version\":\"1.0\",\"scene_version\":1,\"entities\":["
                + "{\"id\":\"sofa_1\",\"components\":{\"entity_name\":{\"value\":\"Sofa\"}}}]},"
        );

        var spec = ScenarioSpecLoader.Load(json);

        spec.StartingScene.Should().NotBeNull();
        spec.StartingScene!.HasEntity("sofa_1").Should().BeTrue();
    }

    [Fact]
    public void Load_InvalidStartingScene_Throws()
    {
        var json = ValidMinimalSpec.Replace(
            "\"category\":\"new_scene_generation\",",
            "\"category\":\"new_scene_generation\","
                + "\"starting_scene\":{\"version\":\"1.0\",\"scene_version\":1,\"entities\":["
                + "{\"components\":{}}]},"
        );

        var act = () => ScenarioSpecLoader.Load(json);

        act.Should().Throw<ScenarioSpecException>().WithMessage("*starting_scene*");
    }

    #endregion
}
