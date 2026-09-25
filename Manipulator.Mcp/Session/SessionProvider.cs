namespace Manipulator.Mcp.Session;

/// <summary>
/// Owns the one <see cref="SceneSession"/> a server process serves: one process is one run.
/// </summary>
/// <remarks>
/// The streamable HTTP transport is stateless from protocol revision 2026-07-28 onwards — the
/// <c>Mcp-Session-Id</c> header is gone and a tool call carries nothing that identifies a client —
/// so per-client scenes cannot be told apart reliably. The harness starts a server per run, which
/// makes the process the isolation boundary instead; that also keeps runs reproducible, because a
/// leftover session can never bleed into the next one.
/// </remarks>
public sealed class SessionProvider
{
    private readonly Lazy<SceneSession> _session;

    public SessionProvider(RunOptions options, StartingScene startingScene)
    {
        var directory = Path.GetDirectoryName(options.CallLogPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _session = new Lazy<SceneSession>(() => new SceneSession(
            options.RunId,
            startingScene,
            options.CallLogPath
        ));
    }

    public SceneSession Current => _session.Value;
}
