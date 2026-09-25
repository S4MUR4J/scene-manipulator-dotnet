namespace Manipulator.Core.Ecs;

/// <summary>
/// Bounding box (full extents on each axis, not radii) of each <see cref="GeometryType"/>
/// at <c>Transform.Scale == Vector3.One</c>. An entity's actual size is this value multiplied
/// component-wise by its Transform.Scale. This is the single source of truth for primitive
/// sizes — validators and the collision metric compute object sizes from it.
/// </summary>
public static class GeometryBounds
{
    private static readonly IReadOnlyDictionary<GeometryType, Vector3> Bounds = new Dictionary<
        GeometryType,
        Vector3
    >
    {
        [GeometryType.Cube] = new Vector3(1f, 1f, 1f),
        [GeometryType.Sphere] = new Vector3(1f, 1f, 1f),
        [GeometryType.Cylinder] = new Vector3(1f, 1f, 1f),
        [GeometryType.Cone] = new Vector3(1f, 1f, 1f),
        [GeometryType.Capsule] = new Vector3(1f, 1.5f, 1f),
        [GeometryType.Plane] = new Vector3(1f, 0f, 1f),
        [GeometryType.Torus] = new Vector3(1f, 0.3f, 1f),
        [GeometryType.Hemisphere] = new Vector3(1f, 0.5f, 1f),
    };

    public static Vector3 For(GeometryType geometry) => Bounds[geometry];
}
