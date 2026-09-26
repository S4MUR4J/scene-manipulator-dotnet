namespace Manipulator.Mcp.Tools;

/// <summary>
/// Every string the agent reads about the tools. They are part of the experiment's controlled
/// constants: the same wording has to reach every run of every approach, so it lives in one place
/// and is guarded by a snapshot test rather than being spread across attributes.
/// </summary>
internal static class ToolDescriptions
{
    public const string Geometries =
        "Cube, Sphere, Cylinder, Cone, Capsule, Plane, Torus, Hemisphere";

    // Tools.
    public const string AddEntity =
        "Add a new object to the scene and return its generated entity id. Every primitive is 1 unit "
        + "wide and 1 unit deep at scale [1, 1, 1] and is centred on its position; heights are 1 unit "
        + "except Capsule (1.5), Plane (0), Torus (0.3) and Hemisphere (0.5). Fields you omit fall "
        + "back to their defaults.";

    public const string MoveEntity =
        "Move an existing entity to an absolute world position. The position replaces the current "
        + "one, it is not added to it.";

    public const string RotateEntity =
        "Set an existing entity's rotation, in degrees. The rotation replaces the current one, it is "
        + "not added to it.";

    public const string ScaleEntity =
        "Set an existing entity's scale factors. The scale replaces the current one, it is not "
        + "multiplied by it.";

    public const string SetMaterial =
        "Change how an existing entity looks. Only the fields you pass are changed; every field you "
        + "omit keeps its current value.";

    public const string RenameEntity =
        "Change an existing entity's label. The entity id stays the same.";

    public const string RemoveEntity = "Delete an entity from the scene. This cannot be undone.";

    public const string GetScene =
        "Return the whole scene as JSON: the current scene version and every entity with its id and "
        + "its components (transform, mesh_filter, mesh_renderer, entity_name). Use it to check the "
        + "current state before deciding what to change.";

    public const string GetEntity =
        "Return one entity as JSON, by id. Use it to check a single object without reading the whole "
        + "scene.";

    public const string Finish =
        "Call this when the scene is complete and there is nothing left to do. It ends the run; "
        + "any tool call made after it is rejected.";

    // Parameters.
    public const string EntityIdParam =
        "Id of the entity, as returned by add_entity or listed by get_scene.";

    public const string GeometryParam = "Shape of the object. One of: " + Geometries + ".";

    public const string PositionParam =
        "World position of the object's centre as [x, y, z]. Y is up. Defaults to [0, 0, 0].";

    public const string RequiredPositionParam =
        "Target world position of the object's centre as [x, y, z]. Y is up.";

    public const string RotationParam =
        "Rotation in degrees as [x, y, z]. Defaults to [0, 0, 0].";

    public const string RequiredRotationParam = "Rotation in degrees as [x, y, z].";

    public const string ScaleParam =
        "Scale factor per axis as [x, y, z]. Every value must be greater than 0. Defaults to "
        + "[1, 1, 1].";

    public const string RequiredScaleParam =
        "Scale factor per axis as [x, y, z]. Every value must be greater than 0.";

    public const string ColorParam = "Colour as a hex string in #rrggbb form. Defaults to #ffffff.";

    public const string OpacityParam =
        "Opacity from 0 (fully transparent) to 1 (fully solid). Defaults to 1.";

    public const string MetalnessParam =
        "Metalness from 0 (non-metal) to 1 (metal). Defaults to 0.";

    public const string RoughnessParam =
        "Surface roughness from 0 (mirror-like) to 1 (fully diffuse). Defaults to 0.5.";

    public const string NameParam =
        "Human-readable label for the object, for example \"table top\". Defaults to an empty label.";

    public const string MaterialColorParam =
        "New colour as a hex string in #rrggbb form. Omit to keep the current colour.";

    public const string MaterialOpacityParam =
        "New opacity from 0 (fully transparent) to 1 (fully solid). Omit to keep the current opacity.";

    public const string MaterialMetalnessParam =
        "New metalness from 0 (non-metal) to 1 (metal). Omit to keep the current metalness.";

    public const string MaterialRoughnessParam =
        "New surface roughness from 0 (mirror-like) to 1 (fully diffuse). Omit to keep the current "
        + "roughness.";

    public const string NewNameParam = "New human-readable label for the entity.";
}
