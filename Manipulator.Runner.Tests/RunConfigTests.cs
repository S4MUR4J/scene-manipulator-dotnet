using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Manipulator.Runner.Tests;

public class RunConfigTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string> args) =>
        new ConfigurationBuilder().AddInMemoryCollection(args!).Build();

    [Fact]
    public void FromConfiguration_MissingScenarioFile_Throws()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string>());

        var act = () => RunConfig.FromConfiguration(configuration);

        act.Should().Throw<ArgumentException>().WithMessage("*scenario-file*");
    }

    [Fact]
    public void FromConfiguration_NonMcpApproach_Throws()
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string>
            {
                ["scenario-file"] = "scenarios/s1-new-gen-livingroom.json",
                ["approach"] = "dsl",
            }
        );

        var act = () => RunConfig.FromConfiguration(configuration);

        act.Should().Throw<NotSupportedException>().WithMessage("*dsl*");
    }

    [Fact]
    public void FromConfiguration_ValidArgs_AppliesDefaults()
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string>
            {
                ["scenario-file"] = "scenarios/s1-new-gen-livingroom.json",
            }
        );

        var config = RunConfig.FromConfiguration(configuration);

        config.Approach.Should().Be("mcp");
        config.Model.Should().Be("claude-sonnet-5");
        config.RunIndex.Should().Be(0);
        config.Batch.Should().Be("adhoc");
        config.MaxToolIterations.Should().Be(RunnerConstants.DefaultMaxToolIterations);
        config.TimeoutSeconds.Should().BeNull();
        config.Seed.Should().BeNull();
    }

    [Fact]
    public void FromConfiguration_OverridesGivenExplicitly()
    {
        var configuration = BuildConfiguration(
            new Dictionary<string, string>
            {
                ["scenario-file"] = "scenarios/s1-new-gen-livingroom.json",
                ["scenario"] = "1",
                ["variant"] = "2",
                ["model"] = "claude-opus-5",
                ["seed"] = "42",
                ["run-index"] = "3",
                ["batch"] = "pilot",
                ["out-dir"] = "custom-runs",
                ["max-iterations"] = "10",
                ["timeout-s"] = "60",
            }
        );

        var config = RunConfig.FromConfiguration(configuration);

        config.Scenario.Should().Be("1");
        config.Variant.Should().Be("2");
        config.Model.Should().Be("claude-opus-5");
        config.Seed.Should().Be(42);
        config.RunIndex.Should().Be(3);
        config.Batch.Should().Be("pilot");
        config.OutDir.Should().Be("custom-runs");
        config.MaxToolIterations.Should().Be(10);
        config.TimeoutSeconds.Should().Be(60);
    }
}
