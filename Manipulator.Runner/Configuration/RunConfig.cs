namespace Manipulator.Runner.Configuration;

public sealed record RunConfig(
    string Scenario,
    string? Variant,
    string Approach,
    string ModelName,
    ModelProvider Provider,
    string Model,
    int? Seed,
    int RunIndex,
    string ScenarioFile,
    string Batch,
    string OutDir,
    int MaxToolIterations,
    int? TimeoutSeconds
);
