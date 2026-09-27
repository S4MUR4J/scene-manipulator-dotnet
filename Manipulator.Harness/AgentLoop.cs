using System.Diagnostics;
using System.Text.Json.Nodes;
using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;
using Manipulator.Harness.Anthropic;
using Manipulator.Harness.Logging;
using Manipulator.Runner;
using Manipulator.Scenarios.Specs;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Manipulator.Harness;

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
/// tool list to the LLM, executes tool calls through the MCP client, and stops on finish / iteration
/// limit / timeout / fatal error. Identical loop, system prompt and model parameters are meant to be
/// reused by the dsl/text approaches once they exist (MAN-77, MAN-82).
/// </summary>
public sealed class AgentLoop(AnthropicClient anthropicClient)
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

        string? startingScenePath = null;
        if (spec.StartingScene is not null)
        {
            startingScenePath = Path.Combine(
                Path.GetTempPath(),
                $"manipulator-harness-{runId}.json"
            );
            File.WriteAllText(startingScenePath, SceneSerializer.Serialize(spec.StartingScene));
        }

        var hostArgs = new List<string> { "--urls", "http://127.0.0.1:0" };
        if (startingScenePath is not null)
            hostArgs.AddRange(["--starting-scene", startingScenePath]);

        var app = RunnerHost.Build(hostArgs.ToArray());
        await app.StartAsync(cancellationToken);

        var scene = app.Services.GetRequiredService<Scene>();
        var runnerState = app.Services.GetRequiredService<RunnerState>();
        var baseAddress = app
            .Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        var toolCallsByTool = new Dictionary<string, int>();
        var toolErrors = new List<ToolErrorLog>();
        var llmCalls = 0;
        var inputTokens = 0;
        var outputTokens = 0;
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
                    Name = "manipulator-harness",
                },
                NullLoggerFactory.Instance
            );
            mcpClient = await McpClient.CreateAsync(
                transport,
                cancellationToken: cancellationToken
            );

            var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
            var tools = new JsonArray();
            foreach (var tool in mcpTools)
            {
                tools.Add(
                    new JsonObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        ["input_schema"] = JsonNode.Parse(tool.JsonSchema.GetRawText()),
                    }
                );
            }

            var promptText = ResolvePrompt(spec, config.Variant);
            var messages = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject { ["type"] = "text", ["text"] = promptText },
                    },
                },
            };

            var timeoutSeconds = config.TimeoutSeconds ?? spec.TimeoutSeconds;

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

                AnthropicResponse response;
                try
                {
                    response = await anthropicClient.SendAsync(
                        config.Model,
                        HarnessConstants.SystemPrompt,
                        messages,
                        tools,
                        cancellationToken
                    );
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

                messages.Add(
                    new JsonObject
                    {
                        ["role"] = "assistant",
                        ["content"] = response.Content.DeepClone(),
                    }
                );

                var toolUses = response
                    .Content.OfType<JsonObject>()
                    .Where(block => block["type"]?.GetValue<string>() == "tool_use")
                    .ToList();

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

                var resultBlocks = new JsonArray();
                foreach (var toolUse in toolUses)
                {
                    var toolName = toolUse["name"]!.GetValue<string>();
                    var toolUseId = toolUse["id"]!.GetValue<string>();
                    var input = toolUse["input"] as JsonObject ?? new JsonObject();
                    var arguments = input.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

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
                        new ToolCallLog(toolName, input.DeepClone(), isError, errorMessage)
                    );

                    resultBlocks.Add(
                        new JsonObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = toolUseId,
                            ["content"] = resultText,
                            ["is_error"] = isError,
                        }
                    );
                }

                messages.Add(new JsonObject { ["role"] = "user", ["content"] = resultBlocks });

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
            if (startingScenePath is not null)
                File.Delete(startingScenePath);
        }

        var finalSceneJson = SceneSerializer.Serialize(scene);
        var endTime = DateTimeOffset.UtcNow;

        return new RunRecord(
            runId,
            new RunConfigLog(
                config.Scenario,
                config.Variant,
                config.Approach,
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
            JsonNode.Parse(finalSceneJson)
        );
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
