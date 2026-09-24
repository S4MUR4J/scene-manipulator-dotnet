using System.Text.Json;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Core.Ecs.Components.Validators;

namespace Manipulator.Core.Serialization;

public class SceneSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    private static readonly MeshRendererValidator MeshRendererValidator =
        new MeshRendererValidator();

    private static readonly string[] TransformFields = ["position", "rotation", "scale"];
    private static readonly string[] MeshFilterFields = ["geometry", "parameters"];
    private static readonly string[] MeshRendererFields =
    [
        "color",
        "opacity",
        "metalness",
        "roughness",
    ];
    private static readonly string[] EntityNameFields = ["value"];

    public static string Serialize(Scene scene)
    {
        var dto = new SceneDto(
            Version: "1.0",
            SceneVersion: scene.Version,
            Entities: scene.Entities.Values.Select(ToEntityDto).ToList()
        );
        return JsonSerializer.Serialize(dto, Options);
    }

    public static SceneDeserializationResult Deserialize(string json)
    {
        SceneDto dto;
        try
        {
            dto =
                JsonSerializer.Deserialize<SceneDto>(json, Options)
                ?? throw new SceneDeserializationException("JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new SceneDeserializationException($"Invalid JSON: {ex.Message}", ex);
        }

        if (dto.Entities is null)
            throw new SceneDeserializationException("Missing required field 'entities'.");

        var scene = new Scene();
        var seenIds = new HashSet<string>();
        var warnings = new List<string>();

        foreach (var entityDto in dto.Entities)
        {
            var entityId =
                entityDto.Id
                ?? throw new SceneDeserializationException(
                    "Entity is missing required field 'id'."
                );

            if (!seenIds.Add(entityId))
                throw new SceneDeserializationException($"Duplicate entity id '{entityId}'.");

            Entity entity;
            try
            {
                entity = new Entity(entityId);
            }
            catch (Exception ex) when (ex is not SceneDeserializationException)
            {
                throw new SceneDeserializationException(
                    $"Entity '{entityId}': invalid id. {ex.Message}",
                    ex
                );
            }

            if (entityDto.Components is null)
                throw new SceneDeserializationException(
                    $"Entity '{entityId}' is missing required field 'components'."
                );

            foreach (var (typeName, element) in entityDto.Components)
            {
                IComponent? component;
                try
                {
                    component = DeserializeComponent(entityId, typeName, element, warnings);
                }
                catch (SceneDeserializationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SceneDeserializationException(
                        $"Entity '{entityId}', component '{typeName}': {ex.Message}",
                        ex
                    );
                }

                if (component is not null)
                    entity.Set(component.Type, component);
            }

            scene.AddEntity(entity);
        }

        scene.RestoreVersion(dto.SceneVersion);

        return new SceneDeserializationResult(scene, warnings);
    }

    private static EntityDto ToEntityDto(Entity entity) =>
        new EntityDto(
            Id: entity.Id,
            Components: entity.Components.ToDictionary(
                kvp => JsonNamingPolicy.SnakeCaseLower.ConvertName(kvp.Key),
                kvp => SerializeComponent(kvp.Value)
            )
        );

    private static JsonElement SerializeComponent(IComponent component)
    {
        object dto = component switch
        {
            Transform t => new TransformDto(
                Position: [t.Position.X, t.Position.Y, t.Position.Z],
                Rotation: [t.Rotation.X, t.Rotation.Y, t.Rotation.Z],
                Scale: [t.Scale.X, t.Scale.Y, t.Scale.Z]
            ),
            MeshFilter mf => new MeshFilterDto(
                Geometry: mf.Geometry.ToString(),
                Parameters: mf.Parameters is null
                    ? null
                    : new Dictionary<string, object>(mf.Parameters)
            ),
            MeshRenderer mr => new MeshRendererDto(
                Color: mr.Color,
                Opacity: mr.Opacity,
                Metalness: mr.Metalness,
                Roughness: mr.Roughness
            ),
            EntityName en => new EntityNameDto(Value: en.Value),
            var _ => new { },
        };
        var raw = JsonSerializer.Serialize(dto, Options);
        return JsonSerializer.Deserialize<JsonElement>(raw, Options);
    }

    private static IComponent? DeserializeComponent(
        string entityId,
        string typeName,
        JsonElement element,
        List<string> warnings
    )
    {
        var knownFields = typeName switch
        {
            "transform" => TransformFields,
            "mesh_filter" => MeshFilterFields,
            "mesh_renderer" => MeshRendererFields,
            "entity_name" => EntityNameFields,
            var _ => null,
        };

        if (knownFields is null)
        {
            warnings.Add($"Entity '{entityId}': unknown component type '{typeName}' was ignored.");
            return null;
        }

        ReportUnknownFields(entityId, typeName, element, knownFields, warnings);

        var raw = element.GetRawText();
        return typeName switch
        {
            "transform" => ToTransform(
                entityId,
                JsonSerializer.Deserialize<TransformDto>(raw, Options)!
            ),
            "mesh_filter" => ToMeshFilter(
                entityId,
                JsonSerializer.Deserialize<MeshFilterDto>(raw, Options)!
            ),
            "mesh_renderer" => ToMeshRenderer(
                entityId,
                JsonSerializer.Deserialize<MeshRendererDto>(raw, Options)!
            ),
            "entity_name" => ToEntityName(
                entityId,
                JsonSerializer.Deserialize<EntityNameDto>(raw, Options)!
            ),
            var _ => null,
        };
    }

    private static void ReportUnknownFields(
        string entityId,
        string componentType,
        JsonElement element,
        string[] knownFields,
        List<string> warnings
    )
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            if (!knownFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                warnings.Add(
                    $"Entity '{entityId}', component '{componentType}': unknown field '{property.Name}' was ignored."
                );
        }
    }

    private static Transform ToTransform(string entityId, TransformDto dto) =>
        new Transform
        {
            Position = ParseVector(entityId, "transform", "position", dto.Position, Vector3.Zero),
            Rotation = ParseVector(entityId, "transform", "rotation", dto.Rotation, Vector3.Zero),
            Scale = ParseVector(entityId, "transform", "scale", dto.Scale, Vector3.One),
        };

    private static Vector3 ParseVector(
        string entityId,
        string componentType,
        string fieldName,
        float[]? values,
        Vector3 defaultValue
    )
    {
        if (values is null)
            return defaultValue;

        if (values.Length != 3)
            throw Fail(
                entityId,
                componentType,
                $"field '{fieldName}' must have exactly 3 numbers, got {values.Length}."
            );

        var vector = new Vector3(values[0], values[1], values[2]);
        if (!vector.IsFinite())
            throw Fail(
                entityId,
                componentType,
                $"field '{fieldName}' must contain finite numbers, got [{string.Join(", ", values)}]."
            );

        return vector;
    }

    private static MeshFilter ToMeshFilter(string entityId, MeshFilterDto dto)
    {
        if (dto.Geometry is null)
            throw Fail(entityId, "mesh_filter", "missing required field 'geometry'.");

        if (!Enum.TryParse<GeometryType>(dto.Geometry, ignoreCase: true, out var geometry))
            throw Fail(
                entityId,
                "mesh_filter",
                $"unknown geometry '{dto.Geometry}'. Valid values: {string.Join(", ", Enum.GetNames<GeometryType>())}."
            );

        return new MeshFilter(geometry, dto.Parameters);
    }

    private static MeshRenderer ToMeshRenderer(string entityId, MeshRendererDto dto)
    {
        var meshRenderer = new MeshRenderer(
            Color: dto.Color ?? "#ffffff",
            Opacity: dto.Opacity ?? 1.0f,
            Metalness: dto.Metalness ?? 0.0f,
            Roughness: dto.Roughness ?? 0.5f
        );

        var validation = MeshRendererValidator.Validate(meshRenderer);
        if (!validation.IsValid)
            throw Fail(
                entityId,
                "mesh_renderer",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            );

        return meshRenderer;
    }

    private static EntityName ToEntityName(string entityId, EntityNameDto dto) =>
        new EntityName(dto.Value ?? "");

    private static SceneDeserializationException Fail(
        string entityId,
        string componentType,
        string message
    ) =>
        new SceneDeserializationException(
            $"Entity '{entityId}', component '{componentType}': {message}"
        );
}
