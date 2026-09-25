using Manipulator.Core.Serialization;
using Manipulator.Mcp.Session;
using Manipulator.Mcp.Tools;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

var runOptions = RunOptions.FromConfiguration(builder.Configuration);

builder.Services.AddSingleton(runOptions);
builder.Services.AddSingleton<StartingScene>();
builder.Services.AddSingleton<SessionProvider>();
builder.Services.AddSingleton<ToolGateway>();

builder
    .Services.AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "manipulator-scene",
            Version = "1.0.0",
        };
    })
    .WithHttpTransport()
    .WithTools<SceneReadTools>()
    .WithTools<SceneWriteTools>()
    .WithTools<SessionTools>();

var app = builder.Build();

// Build the session before accepting connections: a broken starting scene must stop the run rather
// than quietly hand the agent an empty scene, and the run's log starts at process start, not at the
// agent's first call.
var session = app.Services.GetRequiredService<SessionProvider>().Current;

app.MapMcp("/mcp");

// Read-only hooks for the harness. The JSONL call log is the primary record; these serve a harness
// that did not pass --call-log, and let one be polled while a run is in flight.
app.MapGet(
    "/session",
    () =>
        Results.Ok(
            new
            {
                id = session.Id,
                started_at = session.StartedAt,
                last_activity_at = session.LastActivityAt,
                finished = session.IsFinished,
                tool_calls = session.ToolCallCount,
                scene_version = session.Scene.Version,
                entity_count = session.Scene.Count,
            }
        )
);

app.MapGet("/session/log", () => Results.Json(session.CallLog.ToJson()));

app.MapGet(
    "/session/scene",
    () => Results.Text(SceneSerializer.Serialize(session.Scene), "application/json")
);

app.Run();

public partial class Program;
