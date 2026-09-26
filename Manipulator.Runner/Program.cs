using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Mcp;

var builder = WebApplication.CreateBuilder(args);

var scene = new Scene();
var eventBus = new EventBus();
var dispatcher = CommandDispatcherFactory.Create(scene, eventBus);

builder.Services.AddSingleton(scene);
builder.Services.AddSingleton(eventBus);
builder.Services.AddSingleton(dispatcher);

builder.Services.AddManipulatorMcp();

var app = builder.Build();

app.MapManipulatorMcp();

app.Run();
