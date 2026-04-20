namespace Manipulator.Core.Ecs.Components;

public record Transform : IComponent
{
    public string Type { get; } = nameof(Transform);
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Vector3 Rotation { get; init; } = Vector3.Zero;
    public Vector3 Scale { get; init; } = Vector3.One;
}
