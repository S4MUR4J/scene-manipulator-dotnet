using System.Text.Json;

namespace Manipulator.Core.Serialization;

record SceneDto(string Version, long SceneVersion, List<EntityDto> Entities);

record EntityDto(string Id, Dictionary<string, JsonElement> Components);

record TransformDto(float[] Position, float[] Rotation, float[] Scale);

record MeshFilterDto(string Geometry);

record MeshRendererDto(string Color, float Opacity, float Metalness, float Roughness);

record EntityNameDto(string Value);
