using System.Text.Json.Nodes;

namespace Manipulator.Mcp.Tests;

public static class ToolResponse
{
    public static JsonNode Parse(string json) => JsonNode.Parse(json)!;
}
