using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Core.Serialization;
using Manipulator.Mcp;
using Manipulator.Runner.Configuration;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Serilog;
using Serilog.Formatting.Compact;

namespace Manipulator.Runner.Mcp;

/// <summary>Creates the isolated MCP server used by one research scenario run.</summary>
static class ScenarioMcpHost
{
    public static WebApplication Build(
        Scene? startingScene,
        string approach = Approaches.Mcp,
        string? runId = null
    )
    {
        var builder = WebApplication.CreateBuilder();
        var diagnosticLogName = runId ?? Guid.NewGuid().ToString("n");
        var diagnosticLogPath = Path.Combine("logs", $"mcp-host-{diagnosticLogName}.jsonl");

        builder.Host.UseSerilog(
            (_, configuration) =>
                configuration
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(new CompactJsonFormatter(), diagnosticLogPath)
        );

        var scene = CloneOrCreateScene(startingScene);
        var eventBus = new EventBus();
        var dispatcher = CommandDispatcherFactory.Create(scene, eventBus);

        builder.Services.AddSingleton(scene);
        builder.Services.AddSingleton(eventBus);
        builder.Services.AddSingleton(dispatcher);
        builder.Services.AddSingleton<ScenarioRunState>();

        builder.Services.Configure<McpServerOptions>(options =>
        {
            options.Filters.Request.CallToolFilters.Add(next =>
                async (context, cancellationToken) =>
                {
                    var logger = context.Services!.GetRequiredService<
                        ILogger<ScenarioMcpHostLog>
                    >();
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    var result = await next(context, cancellationToken);

                    var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
                    var envelope = ToolEnvelope.TryParse(text);
                    var code =
                        envelope?["code"]?.GetValue<int>() ?? (result.IsError == true ? 500 : 200);
                    var error = envelope?["error"]?.GetValue<string>();
                    var warnings = ToolEnvelope.Warnings(envelope);

                    logger.Log(
                        code >= 400 ? LogLevel.Warning : LogLevel.Information,
                        "{ToolName} -> {Code} in {ElapsedMs}ms {Error}",
                        context.Params.Name,
                        code,
                        stopwatch.ElapsedMilliseconds,
                        error
                    );
                    if (warnings.Count > 0)
                        logger.LogWarning(
                            "{ToolName} warnings: {@Warnings}",
                            context.Params.Name,
                            warnings
                        );

                    return result;
                }
            );
        });

        var mcpServer =
            approach == Approaches.Text
                ? builder.Services.AddManipulatorTextMcp()
                : builder.Services.AddManipulatorMcp();
        mcpServer.WithTools<FinishTool>();

        var app = builder.Build();

        var eventLogger = app.Services.GetRequiredService<ILogger<ScenarioMcpHostLog>>();
        eventBus.Subscribe<ISceneEvent>(sceneEvent =>
            eventLogger.LogInformation(
                "Scene event {EventType}: {@Event}",
                sceneEvent.GetType().Name,
                sceneEvent
            )
        );

        app.MapManipulatorMcp();

        return app;
    }

    private static Scene CloneOrCreateScene(Scene? startingScene) =>
        startingScene is null
            ? new Scene()
            : SceneSerializer.Deserialize(SceneSerializer.Serialize(startingScene)).Scene;
}

sealed class ScenarioMcpHostLog;
