namespace Manipulator.Core.Ecs.Components;

public record MeshRenderer(
    string Color = "#ffffff",
    float Opacity = 1.0f,
    float Metalness = 0.0f,
    float Roughness = 0.5f
) : IComponent
{
    public string Type => nameof(MeshRenderer);
};
