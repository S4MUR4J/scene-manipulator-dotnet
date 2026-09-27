using FluentAssertions;

namespace Manipulator.Harness.Tests;

public class RunConfigTests
{
    [Fact]
    public void FromArgs_MissingScenarioFile_Throws()
    {
        var args = new Dictionary<string, string>();

        var act = () => RunConfig.FromArgs(args);

        act.Should().Throw<ArgumentException>().WithMessage("*scenario-file*");
    }

    [Fact]
    public void FromArgs_NonMcpApproach_Throws()
    {
        var args = new Dictionary<string, string>
        {
            ["scenario-file"] = "scenarios/s1-new-gen-livingroom.json",
            ["approach"] = "dsl",
        };

        var act = () => RunConfig.FromArgs(args);

        act.Should().Throw<NotSupportedException>().WithMessage("*dsl*");
    }

    [Fact]
    public void FromArgs_ValidArgs_AppliesDefaults()
    {
        var args = new Dictionary<string, string>
        {
            ["scenario-file"] = "scenarios/s1-new-gen-livingroom.json",
        };

        var config = RunConfig.FromArgs(args);

        config.Approach.Should().Be("mcp");
        config.Model.Should().Be("claude-sonnet-5");
        config.RunIndex.Should().Be(0);
        config.Batch.Should().Be("adhoc");
        config.MaxToolIterations.Should().Be(HarnessConstants.DefaultMaxToolIterations);
        config.TimeoutSeconds.Should().BeNull();
        config.Seed.Should().BeNull();
    }

    [Fact]
    public void FromArgs_OverridesGivenExplicitly()
    {
        var args = new Dictionary<string, string>
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
        };

        var config = RunConfig.FromArgs(args);

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
