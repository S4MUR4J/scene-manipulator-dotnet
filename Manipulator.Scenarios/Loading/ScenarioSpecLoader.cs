using System.Text.Json;
using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;
using Manipulator.Scenarios.Contracts;
using Manipulator.Scenarios.Specs;
using Manipulator.Scenarios.Validation;

namespace Manipulator.Scenarios.Loading;

public static class ScenarioSpecLoader
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static ScenarioSpec LoadFile(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new ScenarioSpecException($"Could not read spec file '{path}': {ex.Message}", ex);
        }

        try
        {
            return Load(json);
        }
        catch (ScenarioSpecException ex)
        {
            throw new ScenarioSpecException($"{path}: {ex.Message}", ex);
        }
    }

    public static ScenarioSpec Load(string json)
    {
        ScenarioSpecDto dto;
        try
        {
            dto =
                JsonSerializer.Deserialize<ScenarioSpecDto>(json, Options)
                ?? throw new ScenarioSpecException("JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new ScenarioSpecException($"Invalid JSON: {ex.Message}", ex);
        }

        var result = ScenarioSpecDtoValidator.ValidateSpec(dto);
        if (!result.IsValid)
            throw new ScenarioSpecException(
                string.Join(" ", result.Errors.Select(e => e.ErrorMessage))
            );

        var startingScene = ToStartingScene(dto.StartingScene);

        return new ScenarioSpec(
            dto.Id!,
            dto.Category!,
            dto.Prompt!,
            dto.Variants ?? [],
            startingScene,
            ToRoles(dto.Roles!),
            dto.Requirements!.Select(r => r.ToDomain()).ToList(),
            dto.IterationLimit!.Value,
            dto.TimeoutS!.Value,
            dto.Runs!.ToDomain()
        );
    }

    private static Scene? ToStartingScene(JsonElement? element)
    {
        if (element is null)
            return null;

        try
        {
            return SceneSerializer.Deserialize(element.Value.GetRawText()).Scene;
        }
        catch (SceneDeserializationException ex)
        {
            throw new ScenarioSpecException($"'starting_scene': {ex.Message}", ex);
        }
    }

    private static Dictionary<string, RoleSelector> ToRoles(
        Dictionary<string, RoleSelectorDto> rolesDto
    )
    {
        var roles = new Dictionary<string, RoleSelector>();
        foreach (var (name, dto) in rolesDto)
            roles[name] = dto.ToDomain();

        return roles;
    }
}
