using FluentAssertions;
using Manipulator.Runner.Configuration;

namespace Manipulator.Runner.Tests;

public class RunConfigTests
{
    [Fact]
    public void ToRunConfigs_MissingScenarioFile_Throws()
    {
        var settings = ValidSettings() with { ScenarioFile = null };

        var act = settings.ToRunConfigs;

        act.Should().Throw<ArgumentException>().WithMessage("*Runner:ScenarioFile*");
    }

    [Fact]
    public void ToRunConfigs_NonMcpApproach_Throws()
    {
        var settings = ValidSettings() with { Approach = "dsl" };

        var act = settings.ToRunConfigs;

        act.Should().Throw<NotSupportedException>().WithMessage("*dsl*");
    }

    [Fact]
    public void ToRunConfigs_NoEnabledModels_Throws()
    {
        var settings = ValidSettings() with
        {
            Models =
            [
                new ModelSettings
                {
                    Name = "sonnet",
                    Provider = "Anthropic",
                    Model = "claude-sonnet-5",
                    Enabled = false,
                },
            ],
        };

        var act = settings.ToRunConfigs;

        act.Should().Throw<ArgumentException>().WithMessage("*enabled model*");
    }

    [Fact]
    public void ToRunConfigs_DuplicateEnabledModelNames_Throws()
    {
        var settings = ValidSettings() with
        {
            Models =
            [
                new ModelSettings
                {
                    Name = "model",
                    Provider = "Anthropic",
                    Model = "claude-sonnet-5",
                },
                new ModelSettings
                {
                    Name = "MODEL",
                    Provider = "OpenAi",
                    Model = "gpt-5",
                },
            ],
        };

        var act = settings.ToRunConfigs;

        act.Should().Throw<ArgumentException>().WithMessage("*duplicate name*");
    }

    [Fact]
    public void ToRunConfigs_ArtifactFilenameCollision_Throws()
    {
        var settings = ValidSettings() with
        {
            Models =
            [
                new ModelSettings
                {
                    Name = "a/b",
                    Provider = "Anthropic",
                    Model = "claude-sonnet-5",
                },
                new ModelSettings
                {
                    Name = "a_b",
                    Provider = "OpenAi",
                    Model = "gpt-5",
                },
            ],
        };

        var act = settings.ToRunConfigs;

        act.Should().Throw<ArgumentException>().WithMessage("*artifact filename*");
    }

    [Fact]
    public void ToRunConfigs_EnabledMatrix_MapsEachProvider()
    {
        var settings = ValidSettings() with
        {
            Models =
            [
                new ModelSettings
                {
                    Name = "sonnet",
                    Provider = "Anthropic",
                    Model = "claude-sonnet-5",
                },
                new ModelSettings
                {
                    Name = "gpt",
                    Provider = "OpenAi",
                    Model = "gpt-5",
                },
                new ModelSettings
                {
                    Name = "disabled",
                    Provider = "OpenAi",
                    Model = "gpt-5-mini",
                    Enabled = false,
                },
            ],
        };

        var configs = settings.ToRunConfigs();

        configs
            .Should()
            .BeEquivalentTo([
                new RunConfig(
                    Scenario: "1",
                    Variant: "2",
                    Approach: "mcp",
                    ModelName: "sonnet",
                    Provider: ModelProvider.Anthropic,
                    Model: "claude-sonnet-5",
                    Seed: 42,
                    RunIndex: 3,
                    ScenarioFile: "scenarios/s1.json",
                    Batch: "pilot",
                    MaxToolIterations: 10,
                    TimeoutSeconds: 60
                ),
                new RunConfig(
                    Scenario: "1",
                    Variant: "2",
                    Approach: "mcp",
                    ModelName: "gpt",
                    Provider: ModelProvider.OpenAi,
                    Model: "gpt-5",
                    Seed: 42,
                    RunIndex: 3,
                    ScenarioFile: "scenarios/s1.json",
                    Batch: "pilot",
                    MaxToolIterations: 10,
                    TimeoutSeconds: 60
                ),
            ]);
    }

    [Fact]
    public void ToRunConfigs_Notes_IsPassedThrough()
    {
        var settings = ValidSettings() with { Notes = "testing prompt variant 2" };

        var configs = settings.ToRunConfigs();

        configs.Should().OnlyContain(config => config.Notes == "testing prompt variant 2");
    }

    [Fact]
    public void ToRunConfigs_NotesOmitted_IsNull()
    {
        var settings = ValidSettings();

        var configs = settings.ToRunConfigs();

        configs.Should().OnlyContain(config => config.Notes == null);
    }

    private static RunnerSettings ValidSettings() =>
        new RunnerSettings
        {
            ScenarioFile = "scenarios/s1.json",
            Scenario = "1",
            Variant = "2",
            Approach = "mcp",
            Seed = 42,
            RunIndex = 3,
            Batch = "pilot",
            MaxToolIterations = 10,
            TimeoutSeconds = 60,
            Models =
            [
                new ModelSettings
                {
                    Name = "sonnet",
                    Provider = "Anthropic",
                    Model = "claude-sonnet-5",
                },
            ],
        };
}
