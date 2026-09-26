using System.Text.Json.Nodes;
using Manipulator.Core.Commands;
using Manipulator.Mcp.Runtime;

namespace Manipulator.Mcp.Tools;

internal static class ToolResults
{
    /// <summary>
    /// Turns a <see cref="CommandResult"/> into the agent's answer. A rejected command is reported
    /// with the validator's or handler's own message, never as an exception, so the agent can fix
    /// its next call.
    /// </summary>
    public static ToolOutcome From(SceneRun run, CommandResult result, string? entityId)
    {
        if (!result.IsSuccess)
            return ToolOutcome.Failure(result.Error ?? "Command failed.");

        var data = new JsonObject { ["scene_version"] = run.Scene.Version };
        if (entityId is not null)
            data["entity_id"] = entityId;

        return ToolOutcome.Success(data);
    }
}
