using Manipulator.Core.Ecs;

namespace Manipulator.Mcp.Tools;

internal static class ToolArgs
{
    private static readonly string ValidGeometries = string.Join(
        ", ",
        Enum.GetNames<GeometryType>()
    );

    public static bool TryGeometry(string? value, out GeometryType geometry, out string? error)
    {
        geometry = default;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"'geometry' is required. Valid values: {ValidGeometries}.";
            return false;
        }

        if (!Enum.TryParse(value, ignoreCase: true, out geometry) || !Enum.IsDefined(geometry))
        {
            error = $"Unknown geometry '{value}'. Valid values: {ValidGeometries}.";
            return false;
        }

        return true;
    }

    public static bool TryVector(
        float[]? values,
        string field,
        out Vector3? vector,
        out string? error
    )
    {
        vector = null;
        error = null;

        if (values is null)
            return true;

        if (values.Length != 3)
        {
            error =
                $"'{field}' must be an array of exactly 3 numbers [x, y, z], got {values.Length}.";
            return false;
        }

        var candidate = new Vector3(values[0], values[1], values[2]);
        if (!candidate.IsFinite())
        {
            error = $"'{field}' must contain finite numbers, got [{string.Join(", ", values)}].";
            return false;
        }

        vector = candidate;
        return true;
    }

    public static bool TryRequiredVector(
        float[]? values,
        string field,
        out Vector3 vector,
        out string? error
    )
    {
        vector = Vector3.Zero;

        if (values is null)
        {
            error = $"'{field}' is required and must be an array of 3 numbers [x, y, z].";
            return false;
        }

        if (!TryVector(values, field, out var parsed, out error))
            return false;

        vector = parsed!.Value;
        return true;
    }

    public static bool TryEntityId(string? entityId, out string id, out string? error)
    {
        id = entityId?.Trim() ?? string.Empty;
        error = null;

        if (id.Length != 0)
            return true;

        error = "'entityId' is required and cannot be empty.";
        return false;
    }
}
