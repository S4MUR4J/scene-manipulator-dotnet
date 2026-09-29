using Manipulator.Runner.Execution;

namespace Manipulator.Runner.Configuration;

public enum ModelProvider
{
    Anthropic,
    OpenAi,
}

public sealed record RunnerSettings
{
    public string? ScenarioFile { get; init; }
    public string? Scenario { get; init; }
    public string? Variant { get; init; }
    public string? Approach { get; init; }
    public int? Seed { get; init; }
    public int RunIndex { get; init; }
    public string? Batch { get; init; }
    public int? MaxToolIterations { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<ModelSettings>? Models { get; init; }

    public IReadOnlyList<RunConfig> ToRunConfigs()
    {
        var scenarioFile = Require(ScenarioFile, "Runner:ScenarioFile");
        var approach = Approach ?? "mcp";
        if (!string.Equals(approach, "mcp", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(
                $"Approach '{approach}' is not implemented yet; only 'mcp' runs end-to-end today."
            );

        var models = Models?.Where(model => model.Enabled).ToList() ?? [];
        if (models.Count == 0)
            throw new ArgumentException("Runner:Models must contain at least one enabled model.");

        var duplicateNames = models
            .GroupBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateNames is not null)
            throw new ArgumentException(
                $"Runner:Models contains duplicate name '{duplicateNames.Key}'."
            );

        var duplicateArtifactNames = models
            .GroupBy(
                model => SanitizeArtifactSegment(model.Name ?? ""),
                StringComparer.OrdinalIgnoreCase
            )
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateArtifactNames is not null)
            throw new ArgumentException(
                $"Runner:Models names produce the same artifact filename '{duplicateArtifactNames.Key}'."
            );

        return
        [
            .. models.Select(model =>
            {
                var modelName = Require(model.Name, "Runner:Models:Name");
                var modelId = Require(model.Model, $"Runner:Models:{modelName}:Model");
                if (!Enum.TryParse<ModelProvider>(model.Provider, true, out var provider))
                    throw new ArgumentException(
                        $"Runner:Models:{modelName}:Provider '{model.Provider}' is unsupported."
                    );

                return new RunConfig(
                    Scenario: Scenario ?? "unknown",
                    Variant: Variant,
                    Approach: approach,
                    ModelName: modelName,
                    Provider: provider,
                    Model: modelId,
                    Seed: Seed,
                    RunIndex: RunIndex,
                    ScenarioFile: scenarioFile,
                    Batch: Batch ?? "adhoc",
                    MaxToolIterations: MaxToolIterations
                        ?? RunnerConstants.DefaultMaxToolIterations,
                    TimeoutSeconds: TimeoutSeconds,
                    Notes: Notes
                );
            }),
        ];
    }

    private static string Require(string? value, string key) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{key} is required.")
            : value;

    private static string SanitizeArtifactSegment(string value) =>
        string.Concat(
            value.Select(character =>
                Path.GetInvalidFileNameChars().Contains(character) ? '_' : character
            )
        );
}

public sealed class ModelSettings
{
    public string? Name { get; init; }
    public string? Provider { get; init; }
    public string? Model { get; init; }
    public bool Enabled { get; init; } = true;
}
