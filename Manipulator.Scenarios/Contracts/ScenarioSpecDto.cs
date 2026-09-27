using System.Text.Json;

namespace Manipulator.Scenarios.Contracts;

record ScenarioSpecDto(
    string? Id,
    string? Category,
    string? Prompt,
    List<string>? Variants,
    JsonElement? StartingScene,
    Dictionary<string, RoleSelectorDto>? Roles,
    List<RequirementDto>? Requirements,
    int? IterationLimit,
    int? TimeoutS,
    RunConfigDto? Runs
);
