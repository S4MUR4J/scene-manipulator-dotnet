using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
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
using ModelContextProtocol.Protocol;
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
/// The MCP-approach agent loop: starts an in-process MCP server session for the run, forwards its
/// tool list to the LLM (via the official <c>Anthropic</c> SDK), executes tool calls through the
/// MCP client, and stops on finish / iteration limit / timeout / fatal error. Identical loop,
/// system prompt and model parameters are meant to be reused by the dsl/text approaches once they
/// exist (MAN-77, MAN-82).
/// </summary>
public sealed class AgentLoop(IModelStrategy modelStrategy)
{
    private static readonly HashSet<string> ReadTools = ["get_scene", "get_entity"];
    private static readonly HashSet<string> WriteTools =
    [
        "add_entity",
        "move_entity",
        "rotate_entity",
        "scale_entity",
        "set_material",
        "rename_entity",
        "remove_entity",
    ];

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

        var app = ScenarioMcpHost.Build(spec.StartingScene);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(cancellationToken);

        var scene = app.Services.GetRequiredService<Scene>();
        var runnerState = app.Services.GetRequiredService<ScenarioRunState>();
        var baseAddress = app
            .Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        var toolCallsByTool = new Dictionary<string, int>();
        var toolErrors = new List<ToolErrorLog>();
        long llmCalls = 0;
        long inputTokens = 0;
        long outputTokens = 0;
        var sceneReads = 0;
        var sceneWrites = 0;
        StopReason stopReason;
        string? fatalError = null;
        McpClient? mcpClient = null;

        try
        {
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri($"{baseAddress}/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp,
                    Name = "manipulator-runner",
                },
                NullLoggerFactory.Instance
            );
            mcpClient = await McpClient.CreateAsync(
                transport,
                cancellationToken: cancellationToken
            );

            var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
            var tools = mcpTools
                .Select(tool => new ModelTool(tool.Name, tool.Description, tool.JsonSchema))
                .ToList();

            var promptText = ResolvePrompt(spec, config.Variant);
            var timeoutSeconds = config.TimeoutSeconds ?? spec.TimeoutSeconds;
            var isFirstTurn = true;
            IReadOnlyList<ModelToolResult>? previousToolResults = null;

            while (true)
            {
                if (stopwatch.Elapsed.TotalSeconds > timeoutSeconds)
                {
                    stopReason = StopReason.Timeout;
                    break;
                }

                if (llmCalls >= config.MaxToolIterations)
                {
                    stopReason = StopReason.IterationLimit;
                    break;
                }

                ModelResponse response;
                try
                {
                    response = isFirstTurn
                        ? await modelStrategy.StartAsync(
                            RunnerConstants.SystemPrompt,
                            promptText,
                            tools,
                            cancellationToken
                        )
                        : await modelStrategy.ContinueAsync(previousToolResults!, cancellationToken);
                    isFirstTurn = false;
                }
                catch (Exception ex)
                {
                    fatalError = ex.Message;
                    stopReason = StopReason.FatalError;
                    break;
                }

                llmCalls++;
                inputTokens += response.InputTokens;
                outputTokens += response.OutputTokens;
                var toolUses = response.ToolCalls;

                var stepCalls = new List<ToolCallLog>();

                if (toolUses.Count == 0)
                {
                    onStep?.Invoke(
                        new StepRecord(
                            runId,
                            llmCalls,
                            response.InputTokens,
                            response.OutputTokens,
                            response.StopReason,
                            stepCalls
                        )
                    );
                    stopReason = StopReason.ModelStoppedNaturally;
                    break;
                }

                var resultBlocks = new List<ModelToolResult>();
                foreach (var toolUse in toolUses)
                {
                    var toolName = toolUse.Name;
                    var toolUseId = toolUse.Id;
                    var arguments = toolUse.Arguments.EnumerateObject().ToDictionary(
                        kv => kv.Name,
                        kv => (object?)kv.Value
                    );

                    toolCallsByTool[toolName] = toolCallsByTool.GetValueOrDefault(toolName) + 1;
                    if (ReadTools.Contains(toolName))
                        sceneReads++;
                    else if (WriteTools.Contains(toolName))
                        sceneWrites++;

                    string resultText;
                    bool isError;
                    string? errorMessage = null;
                    try
                    {
                        var callResult = await mcpClient.CallToolAsync(
                            toolName,
                            arguments,
                            cancellationToken: cancellationToken
                        );
                        resultText =
                            callResult.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text
                            ?? "";
                        var envelope = resultText.Length > 0 ? JsonNode.Parse(resultText) : null;
                        errorMessage = envelope?["error"]?.GetValue<string>();
                        isError = callResult.IsError == true || errorMessage is not null;
                    }
                    catch (Exception ex)
                    {
                        resultText = $"Tool call failed: {ex.Message}";
                        errorMessage = ex.Message;
                        isError = true;
                    }

                    if (isError)
                        toolErrors.Add(
                            new ToolErrorLog(
                                toolName,
                                errorMessage ?? "tool call failed",
                                DateTimeOffset.UtcNow
                            )
                        );

                    stepCalls.Add(
                        new ToolCallLog(
                            toolName,
                            toolUse.Arguments.GetRawText(),
                            isError,
                            errorMessage
                        )
                    );

                    resultBlocks.Add(new ModelToolResult(toolUseId, resultText, isError));
                }
                previousToolResults = resultBlocks;

                onStep?.Invoke(
                    new StepRecord(
                        runId,
                        llmCalls,
                        response.InputTokens,
                        response.OutputTokens,
                        response.StopReason,
                        stepCalls
                    )
                );

                if (runnerState.IsFinished)
                {
                    stopReason = StopReason.FinishCalled;
                    break;
                }
            }
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

        return new RunRecord(
            runId,
            new RunConfigLog(
                config.Scenario,
                config.Variant,
                config.Approach,
                config.ModelName,
                config.Provider.ToString(),
                config.Model,
                config.Seed,
                config.RunIndex
            ),
            startTime,
            endTime,
            stopwatch.Elapsed.TotalMilliseconds,
            stopReason.ToString(),
            fatalError,
            llmCalls,
            inputTokens,
            outputTokens,
            toolCallsByTool.Values.Sum(),
            toolCallsByTool,
            sceneReads,
            sceneWrites,
            toolErrors,
            finalSceneJson
        );
    }

    internal static Anthropic.Models.Messages.InputSchema ToInputSchema(JsonElement mcpJsonSchema)
    {
        var properties = mcpJsonSchema.TryGetProperty("properties", out var props)
            ? props.EnumerateObject().ToDictionary(p => p.Name, p => p.Value)
            : new Dictionary<string, JsonElement>();
        var required = mcpJsonSchema.TryGetProperty("required", out var req)
            ? req.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [];

        return new Anthropic.Models.Messages.InputSchema { Properties = properties, Required = required };
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
