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
            Models = [new ModelSettings { Name = "sonnet", Provider = "Anthropic", Model = "claude-sonnet-5", Enabled = false }],
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
                new ModelSettings { Name = "model", Provider = "Anthropic", Model = "claude-sonnet-5" },
                new ModelSettings { Name = "MODEL", Provider = "OpenAi", Model = "gpt-5" },
            ],
        };

        var act = settings.ToRunConfigs;

        act.Should().Throw<ArgumentException>().WithMessage("*duplicate name*");
    }

    [Fact]
    public void ToRunConfigs_EnabledMatrix_MapsEachProvider()
    {
        var settings = ValidSettings() with
        {
            Models =
            [
                new ModelSettings { Name = "sonnet", Provider = "Anthropic", Model = "claude-sonnet-5" },
                new ModelSettings { Name = "gpt", Provider = "OpenAi", Model = "gpt-5" },
                new ModelSettings { Name = "disabled", Provider = "OpenAi", Model = "gpt-5-mini", Enabled = false },
            ],
        };

        var configs = settings.ToRunConfigs();

        configs.Should()
            .BeEquivalentTo(
                [
                    new RunConfig(
                        "1",
                        "2",
                        "mcp",
                        "sonnet",
                        ModelProvider.Anthropic,
                        "claude-sonnet-5",
                        42,
                        3,
                        "scenarios/s1.json",
                        "pilot",
                        "custom-runs",
                        10,
                        60
                    ),
                    new RunConfig(
                        "1",
                        "2",
                        "mcp",
                        "gpt",
                        ModelProvider.OpenAi,
                        "gpt-5",
                        42,
                        3,
                        "scenarios/s1.json",
                        "pilot",
                        "custom-runs",
                        10,
                        60
                    ),
                ]
            );
    }

    private static RunnerSettings ValidSettings() =>
        new()
        {
            ScenarioFile = "scenarios/s1.json",
            Scenario = "1",
            Variant = "2",
            Approach = "mcp",
            Seed = 42,
            RunIndex = 3,
            Batch = "pilot",
            OutDir = "custom-runs",
            MaxToolIterations = 10,
            TimeoutSeconds = 60,
            Models = [new ModelSettings { Name = "sonnet", Provider = "Anthropic", Model = "claude-sonnet-5" }],
        };
}
