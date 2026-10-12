using System.Text.Json;
using System.Text.Json.Nodes;

namespace Manipulator.Runner.Mcp;

/// <summary>
/// Reads the JSON envelope our tools return. Errors raised by the MCP SDK itself (for example an
/// argument that doesn't bind to the parameter type) come back as plain text, not an envelope.
/// </summary>
static class ToolEnvelope
{
    public static JsonNode? TryParse(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static IReadOnlyList<string> Warnings(JsonNode? envelope) =>
        envelope
            ?["result"]?["warnings"]?.AsArray()
            .Select(warning => warning!.GetValue<string>())
            .ToList()
        ?? [];
}
