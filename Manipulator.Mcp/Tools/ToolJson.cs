using System.Text.Json.Nodes;

namespace Manipulator.Mcp.Tools;

internal static class ToolJson
{
    /// <summary>Builds the argument record for the call log, dropping arguments the agent omitted.</summary>
    public static JsonObject Args(params (string Key, JsonNode? Value)[] values)
    {
        var node = new JsonObject();
        foreach (var (key, value) in values)
        {
            if (value is not null)
                node[key] = value;
        }

        return node;
    }

    public static JsonNode? Vector(float[]? values)
    {
        return values is null
            ? null
            : new JsonArray(values.Select(value => (JsonNode)JsonValue.Create(value)).ToArray());
    }

    public static JsonNode? Value(string? value)
    {
        return value is null ? null : JsonValue.Create(value);
    }

    public static JsonNode? Value(float? value)
    {
        return value is null ? null : JsonValue.Create(value.Value);
    }
}
