using Manipulator.Mcp.Runtime;
using Manipulator.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(RunOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<SceneRun>();

builder
    .Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SceneReadTools>()
    .WithTools<SceneWriteTools>()
    .WithTools<RunTools>();

var app = builder.Build();

// Build the run before accepting connections: a broken starting scene must stop the process rather
// than quietly hand the agent an empty scene, and the log starts at process start, not at the
// agent's first call.
var run = app.Services.GetRequiredService<SceneRun>();

app.MapMcp("/mcp");

app.MapGet(
    "/health",
    () =>
        Results.Ok(
            new
            {
                run_id = run.Id,
                entity_count = run.Scene.Count,
                finished = run.IsFinished,
            }
        )
);

app.Run();
