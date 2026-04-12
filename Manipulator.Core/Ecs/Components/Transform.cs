namespace Manipulator.Core.Ecs.Components;

public record Transform(Vector3? Position = null, Vector3? Rotation = null, Vector3? Scale = null) : IComponent
{
    public string Type { get; } = nameof(Transform);
    public Vector3? Position { get; init; } = Position ?? Vector3.Zero;
    public Vector3? Rotation { get; init; } = Position ?? Vector3.Zero;
    public Vector3? Scale { get; init; } = Position ?? Vector3.One;
}
