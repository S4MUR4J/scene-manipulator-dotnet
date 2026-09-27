using Manipulator.Core.Ecs;

namespace Manipulator.Scenarios.Specs;

/// <summary>
/// A machine-readable spec for one experiment scenario: the prompt(s) to send the agent, the
/// roles requirements can refer to, the requirements a validator scores the resulting scene
/// against, and the run parameters (iteration limit, timeout, N).
/// </summary>
public sealed record ScenarioSpec(
    string Id,
    string Category,
    string Prompt,
    IReadOnlyList<string> Variants,
    Scene? StartingScene,
    IReadOnlyDictionary<string, RoleSelector> Roles,
    IReadOnlyList<Requirement> Requirements,
    int IterationLimit,
    int TimeoutSeconds,
    RunConfig Runs
);
