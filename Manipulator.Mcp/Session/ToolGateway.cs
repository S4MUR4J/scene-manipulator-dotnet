using System.Text.Json.Nodes;

namespace Manipulator.Mcp.Session;

/// <summary>
/// Single entry point every tool goes through: it logs the call with its arguments and renders the
/// result envelope the agent reads.
/// </summary>
/// <remarks>
/// Tools pass their name and arguments explicitly rather than taking the SDK's
/// <c>RequestContext</c>: an injected context parameter was observed leaking into a tool's
/// published input schema, and those schemas are a controlled constant of the experiment — they
/// have to be identical on every run.
/// </remarks>
public sealed class ToolGateway(SessionProvider sessions)
{
    public string Execute(
        string toolName,
        JsonObject arguments,
        Func<SceneSession, ToolOutcome> body
    )
    {
        return sessions.Current.Invoke(toolName, arguments, body);
    }
}
