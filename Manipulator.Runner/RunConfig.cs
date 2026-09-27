namespace Manipulator.Runner;

public sealed record RunConfig(
    string Scenario,
    string? Variant,
    string Approach,
    string Model,
    int? Seed,
    int RunIndex,
    string ScenarioFile,
    string Batch,
    string OutDir,
    int MaxToolIterations,
    int? TimeoutSeconds
)
{
    /// <summary>
    /// Reads run config from <paramref name="configuration"/> - appsettings.json holds the
    /// defaults for day-to-day runs, command-line args (added last, highest precedence in
    /// Program.cs) override them for one-off variations.
    /// </summary>
    public static RunConfig FromConfiguration(IConfiguration configuration)
    {
        var approach = configuration["approach"] ?? "mcp";
        if (approach != "mcp")
            throw new NotSupportedException(
                $"Approach '{approach}' is not implemented yet - only 'mcp' runs end-to-end "
                    + "today (see MAN-77 for text, MAN-82 for dsl)."
            );

        var scenarioFile = configuration["scenario-file"];
        if (string.IsNullOrEmpty(scenarioFile))
            throw new ArgumentException("--scenario-file is required.");

        return new RunConfig(
            Scenario: configuration["scenario"] ?? "unknown",
            Variant: configuration["variant"],
            Approach: approach,
            Model: configuration["model"] ?? "claude-sonnet-5",
            Seed: configuration["seed"] is { } seed ? int.Parse(seed) : null,
            RunIndex: configuration["run-index"] is { } runIndex ? int.Parse(runIndex) : 0,
            ScenarioFile: scenarioFile,
            Batch: configuration["batch"] ?? "adhoc",
            OutDir: configuration["out-dir"] ?? "runs",
            MaxToolIterations: configuration["max-iterations"] is { } maxIter
                ? int.Parse(maxIter)
                : RunnerConstants.DefaultMaxToolIterations,
            TimeoutSeconds: configuration["timeout-s"] is { } timeout ? int.Parse(timeout) : null
        );
    }
}
