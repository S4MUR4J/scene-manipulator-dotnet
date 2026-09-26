using Manipulator.Core.Commands;
using Manipulator.Core.Events;
using Manipulator.Mcp;
using Manipulator.Runner;
using Manipulator.Runner.Tools;

var builder = WebApplication.CreateBuilder(args);

var scene = StartingScene.Load(builder.Configuration["starting-scene"]);
var eventBus = new EventBus();
var dispatcher = CommandDispatcherFactory.Create(scene, eventBus);

builder.Services.AddSingleton(scene);
builder.Services.AddSingleton(eventBus);
builder.Services.AddSingleton(dispatcher);
builder.Services.AddSingleton<RunnerState>();

builder.Services.AddManipulatorMcp().WithTools<FinishTool>();

var app = builder.Build();

app.MapManipulatorMcp();

app.Run();
