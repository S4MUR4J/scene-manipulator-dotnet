using Manipulator.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddManipulatorMcp();

var app = builder.Build();

app.MapManipulatorMcp();

app.Run();
