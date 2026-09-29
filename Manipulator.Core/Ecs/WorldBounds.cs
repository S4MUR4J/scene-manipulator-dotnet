using Manipulator.Core.Ecs.Components;

namespace Manipulator.Core.Ecs;

/// <summary>
/// <see cref="Transform.Position"/> is the entity's center, so the box extends half of the scaled
/// <see cref="GeometryBounds"/> on each side.
/// </summary>
public static class WorldBounds
{
    public static (Vector3 Min, Vector3 Max)? Of(Entity entity)
    {
        var transform = entity.Get<Transform>();
        var meshFilter = entity.Get<MeshFilter>();
        if (transform is null || meshFilter is null)
            return null;

        var halfExtents = GeometryBounds.For(meshFilter.Geometry) * transform.Scale * 0.5f;
        return (transform.Position - halfExtents, transform.Position + halfExtents);
    }
}
