namespace Manipulator.Harness;

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
    public static RunConfig FromArgs(IReadOnlyDictionary<string, string> args)
    {
        var approach = args.GetValueOrDefault("approach", "mcp");
        if (approach != "mcp")
            throw new NotSupportedException(
                $"Approach '{approach}' is not implemented yet - only 'mcp' runs end-to-end "
                    + "today (see MAN-77 for text, MAN-82 for dsl)."
            );

        if (!args.TryGetValue("scenario-file", out var scenarioFile))
            throw new ArgumentException("--scenario-file is required.");

        return new RunConfig(
            Scenario: args.GetValueOrDefault("scenario", "unknown"),
            Variant: args.GetValueOrDefault("variant"),
            Approach: approach,
            Model: args.GetValueOrDefault("model", "claude-sonnet-5"),
            Seed: args.TryGetValue("seed", out var seed) ? int.Parse(seed) : null,
            RunIndex: args.TryGetValue("run-index", out var runIndex) ? int.Parse(runIndex) : 0,
            ScenarioFile: scenarioFile,
            Batch: args.GetValueOrDefault("batch", "adhoc"),
            OutDir: args.GetValueOrDefault("out-dir", "runs"),
            MaxToolIterations: args.TryGetValue("max-iterations", out var maxIter)
                ? int.Parse(maxIter)
                : HarnessConstants.DefaultMaxToolIterations,
            TimeoutSeconds: args.TryGetValue("timeout-s", out var timeout)
                ? int.Parse(timeout)
                : null
        );
    }
}
