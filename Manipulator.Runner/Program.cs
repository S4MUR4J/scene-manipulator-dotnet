using System.Diagnostics;
using System.Text.Json.Nodes;
using Manipulator.Core.Commands;
using Manipulator.Core.Events;
using Manipulator.Mcp;
using Manipulator.Runner;
using Manipulator.Runner.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(
    (context, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                new CompactJsonFormatter(),
                "logs/run-.jsonl",
                rollingInterval: RollingInterval.Day
            )
);

var scene = StartingScene.Load(builder.Configuration["starting-scene"]);
var eventBus = new EventBus();
var dispatcher = CommandDispatcherFactory.Create(scene, eventBus);

builder.Services.AddSingleton(scene);
builder.Services.AddSingleton(eventBus);
builder.Services.AddSingleton(dispatcher);
builder.Services.AddSingleton<RunnerState>();

builder.Services.Configure<McpServerOptions>(options =>
{
    options.Filters.Request.CallToolFilters.Add(next =>
        async (context, cancellationToken) =>
        {
            var logger = context.Services!.GetRequiredService<ILogger<Program>>();
            var stopwatch = Stopwatch.StartNew();

            var result = await next(context, cancellationToken);

            var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
            var envelope = text is null ? null : JsonNode.Parse(text);
            var code = envelope?["code"]?.GetValue<int>() ?? (result.IsError == true ? 500 : 200);
            var error = envelope?["error"]?.GetValue<string>();

            logger.Log(
                code >= 400 ? LogLevel.Warning : LogLevel.Information,
                "{ToolName} -> {Code} in {ElapsedMs}ms {Error}",
                context.Params.Name,
                code,
                stopwatch.ElapsedMilliseconds,
                error
            );

            return result;
        }
    );
});

builder.Services.AddManipulatorMcp().WithTools<FinishTool>();

var app = builder.Build();

var eventLogger = app.Services.GetRequiredService<ILogger<Program>>();
eventBus.Subscribe<ISceneEvent>(sceneEvent =>
    eventLogger.LogInformation(
        "Scene event {EventType}: {@Event}",
        sceneEvent.GetType().Name,
        sceneEvent
    )
);

app.MapManipulatorMcp();

app.Run();
