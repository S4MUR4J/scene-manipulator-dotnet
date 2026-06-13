using System.Text.Json;
using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;

namespace Manipulator.Core.Serialization;

public class SceneSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public static string Serialize(Scene scene)
    {
        var dto = new SceneDto(
            Version: "1.0",
            SceneVersion: scene.Version,
            Entities: scene.Entities.Values.Select(ToEntityDto).ToList()
        );
        return JsonSerializer.Serialize(dto, Options);
    }

    public static Scene Deserialize(string json)
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

        var scene = new Scene();
        var seenIds = new HashSet<string>();

        foreach (var entityDto in dto.Entities)
        {
            if (!seenIds.Add(entityDto.Id))
                throw new SceneDeserializationException($"Duplicate entity ID: {entityDto.Id}");

            var entity = new Entity(entityDto.Id);
            foreach (var (typeName, element) in entityDto.Components)
            {
                var component = DeserializeComponent(typeName, element);
                if (component is not null)
                    entity.Set(component.Type, component);
            }
            scene.AddEntity(entity);
        }

        return scene;
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
            MeshFilter mf => new MeshFilterDto(Geometry: mf.Geometry.ToString()),
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

    private static IComponent? DeserializeComponent(string typeName, JsonElement element)
    {
        var raw = element.GetRawText();
        return typeName switch
        {
            "transform" => ToTransform(JsonSerializer.Deserialize<TransformDto>(raw, Options)!),
            "mesh_filter" => ToMeshFilter(JsonSerializer.Deserialize<MeshFilterDto>(raw, Options)!),
            "mesh_renderer" => ToMeshRenderer(
                JsonSerializer.Deserialize<MeshRendererDto>(raw, Options)!
            ),
            "entity_name" => ToEntityName(JsonSerializer.Deserialize<EntityNameDto>(raw, Options)!),
            var _ => null,
        };
    }

    private static Transform ToTransform(TransformDto dto) =>
        new Transform
        {
            Position = new Vector3(dto.Position[0], dto.Position[1], dto.Position[2]),
            Rotation = new Vector3(dto.Rotation[0], dto.Rotation[1], dto.Rotation[2]),
            Scale = new Vector3(dto.Scale[0], dto.Scale[1], dto.Scale[2]),
        };

    private static MeshFilter ToMeshFilter(MeshFilterDto dto) =>
        new MeshFilter(Geometry: Enum.Parse<GeometryType>(dto.Geometry), Parameters: null);

    private static MeshRenderer ToMeshRenderer(MeshRendererDto dto) =>
        new MeshRenderer(
            Color: dto.Color,
            Opacity: dto.Opacity,
            Metalness: dto.Metalness,
            Roughness: dto.Roughness
        );

    private static EntityName ToEntityName(EntityNameDto dto) => new EntityName(Value: dto.Value);
}
