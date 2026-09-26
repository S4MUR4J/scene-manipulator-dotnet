using Manipulator.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SceneReadTools>()
    .WithTools<SceneWriteTools>();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
