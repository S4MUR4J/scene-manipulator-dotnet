using System.Diagnostics;
using System.Text.Json;
using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;
using Manipulator.Runner.Configuration;
using Manipulator.Runner.Logging;
using Manipulator.Runner.Mcp;
using Manipulator.Runner.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ScenarioSpec = Manipulator.Scenarios.Specs.ScenarioSpec;

namespace Manipulator.Runner.Execution;

public enum StopReason
{
    FinishCalled,
    IterationLimit,
    Timeout,
    FatalError,
    ModelStoppedNaturally,
}

/// <summary>
/// Orchestrates an MCP-based run: hosts the scenario MCP server, gets model responses through
/// an <see cref="IModelStrategy"/>, and stops when the model finishes, reaches a limit, times out,
/// or fails. Tool execution and run telemetry are delegated to dedicated execution types.
/// </summary>
public sealed class AgentLoop(IModelStrategy modelStrategy)
{
    public async Task<RunRecord> RunAsync(
        RunConfig config,
        ScenarioSpec spec,
        Action<StepRecord>? onStep,
        CancellationToken cancellationToken
    )
    {
        var runId = Guid.NewGuid().ToString("n");
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var app = ScenarioMcpHost.Build(spec.StartingScene, runId);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(cancellationToken);

        var scene = app.Services.GetRequiredService<Scene>();
        var runnerState = app.Services.GetRequiredService<ScenarioRunState>();
        var baseAddress = app
            .Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        var metrics = new RunMetrics();
        LoopResult result;
        McpClient? mcpClient = null;

        try
        {
            mcpClient = await CreateMcpClientAsync(baseAddress, cancellationToken);
            var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
            result = await RunLoopAsync(
                config,
                spec,
                mcpClient,
                mcpTools
                    .Select(tool => new ModelTool(tool.Name, tool.Description, tool.JsonSchema))
                    .ToList(),
                runnerState,
                stopwatch,
                metrics,
                runId,
                onStep,
                cancellationToken
            );
        }
        finally
        {
            if (mcpClient is not null)
                await mcpClient.DisposeAsync();
            await app.StopAsync(cancellationToken);
            await app.DisposeAsync();
        }

        var finalSceneJson = SceneSerializer.Serialize(scene);
        var endTime = DateTimeOffset.UtcNow;

        return RunRecord.FromRun(
            runId,
            config,
            startTime,
            endTime,
            stopwatch.Elapsed.TotalMilliseconds,
            result,
            metrics,
            finalSceneJson
        );
    }

    private async Task<LoopResult> RunLoopAsync(
        RunConfig config,
        ScenarioSpec spec,
        McpClient mcpClient,
        IReadOnlyList<ModelTool> tools,
        ScenarioRunState runnerState,
        Stopwatch stopwatch,
        RunMetrics metrics,
        string runId,
        Action<StepRecord>? onStep,
        CancellationToken cancellationToken
    )
    {
        var promptText = ResolvePrompt(spec, config.Variant);
        var timeoutSeconds = config.TimeoutSeconds ?? spec.TimeoutSeconds;
        IReadOnlyList<ModelToolResult> previousToolResults = [];

        while (true)
        {
            if (stopwatch.Elapsed.TotalSeconds > timeoutSeconds)
                return new LoopResult(StopReason.Timeout);

            if (metrics.LlmCalls >= config.MaxToolIterations)
                return new LoopResult(StopReason.IterationLimit);

            ModelResponse response;
            try
            {
                response =
                    metrics.LlmCalls == 0
                        ? await modelStrategy.StartAsync(
                            RunnerConstants.SystemPrompt,
                            promptText,
                            tools,
                            cancellationToken
                        )
                        : await modelStrategy.ContinueAsync(previousToolResults, cancellationToken);
            }
            catch (Exception ex)
            {
                return new LoopResult(StopReason.FatalError, ex.Message);
            }

            metrics.RecordResponse(response);
            var toolExecutions = await ToolCallExecutor.ExecuteAsync(
                mcpClient,
                response.ToolCalls,
                metrics,
                cancellationToken
            );
            onStep?.Invoke(
                StepRecord.FromResponse(
                    runId,
                    metrics.LlmCalls,
                    response,
                    toolExecutions.Select(execution => execution.ToLog()).ToList()
                )
            );

            if (response.ToolCalls.Count == 0)
                return new LoopResult(StopReason.ModelStoppedNaturally);

            previousToolResults = toolExecutions
                .Select(call => new ModelToolResult(call.Id, call.ResultText, call.IsError))
                .ToList();

            if (runnerState.IsFinished)
                return new LoopResult(StopReason.FinishCalled);
        }
    }

    private static async Task<McpClient> CreateMcpClientAsync(
        string baseAddress,
        CancellationToken cancellationToken
    ) =>
        await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri($"{baseAddress}/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp,
                    Name = "manipulator-runner",
                },
                NullLoggerFactory.Instance
            ),
            cancellationToken: cancellationToken
        );

    internal static Anthropic.Models.Messages.InputSchema ToInputSchema(JsonElement mcpJsonSchema)
    {
        var properties = mcpJsonSchema.TryGetProperty("properties", out var props)
            ? props.EnumerateObject().ToDictionary(p => p.Name, p => p.Value)
            : new Dictionary<string, JsonElement>();
        var required = mcpJsonSchema.TryGetProperty("required", out var req)
            ? req.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [];

        return new Anthropic.Models.Messages.InputSchema
        {
            Properties = properties,
            Required = required,
        };
    }

    private static string ResolvePrompt(ScenarioSpec spec, string? variant)
    {
        if (variant is null)
            return spec.Prompt;

        if (!int.TryParse(variant, out var index) || index < 1 || index > spec.Variants.Count)
            throw new ArgumentException(
                $"Variant '{variant}' is not valid for scenario '{spec.Id}' ({spec.Variants.Count} variant(s))."
            );

        return spec.Variants[index - 1];
    }
}
