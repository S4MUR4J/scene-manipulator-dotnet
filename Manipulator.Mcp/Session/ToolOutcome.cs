using System.Text.Json.Nodes;

namespace Manipulator.Mcp.Session;

/// <summary>
/// Result of a single tool body. Failures are values, never exceptions: the agent has to be able to
/// read the error and correct itself, and the harness has to be able to count failures.
/// </summary>
public sealed record ToolOutcome(bool Ok, string? Error = null, JsonObject? Data = null)
{
    public static ToolOutcome Success(JsonObject? data = null)
    {
        return new ToolOutcome(true, null, data);
    }

    public static ToolOutcome Failure(string error)
    {
        return new ToolOutcome(false, error);
    }
}
