using Manipulator.Core.Ecs.Components;

namespace Manipulator.Core.Ecs;

/// <summary>
/// World-space axis-aligned bounding box of an entity: <see cref="Transform.Position"/> is the
/// entity's center, so the box extends by half of <see cref="GeometryBounds"/> (scaled by
/// <see cref="Transform.Scale"/>) on each side.
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
