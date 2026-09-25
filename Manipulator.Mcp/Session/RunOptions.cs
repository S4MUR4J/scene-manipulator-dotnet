namespace Manipulator.Mcp.Session;

/// <summary>
/// Run-level configuration, supplied by the harness on the command line, e.g.
/// <c>--mode mcp --starting-scene scenes/kitchen.json --call-log runs/run-17.jsonl</c>.
/// </summary>
public sealed record RunOptions
{
    public const string McpMode = "mcp";

    /// <summary>
    /// Tool set the server exposes. Only <c>mcp</c> exists today; <c>dsl</c> and <c>text</c>
    /// arrive with their own issues and must fail loudly until then rather than silently
    /// serving the wrong tool set to an experiment run.
    /// </summary>
    public string Mode { get; init; } = McpMode;

    /// <summary>Optional scene every session starts from, for modification and repair scenarios.</summary>
    public string? StartingScenePath { get; init; }

    /// <summary>Optional JSONL file every tool call and scene event is appended to.</summary>
    public string? CallLogPath { get; init; }

    /// <summary>
    /// Identifier the harness gives this run. It tags every log entry so a log can be traced back
    /// to the scenario, approach and model it came from.
    /// </summary>
    public string RunId { get; init; } = "run";

    public static RunOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new RunOptions
        {
            Mode = First(configuration, "mode") ?? McpMode,
            StartingScenePath = First(configuration, "starting-scene", "startingScene"),
            CallLogPath = First(configuration, "call-log", "callLog"),
            RunId = First(configuration, "run-id", "runId") ?? "run",
        };

        if (!string.Equals(options.Mode, McpMode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Unsupported mode '{options.Mode}'. Supported modes: {McpMode}."
            );

        return options with { Mode = McpMode };
    }

    private static string? First(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
