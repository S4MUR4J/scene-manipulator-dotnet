using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Models.Messages;
using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;
using Manipulator.Harness.Logging;
using Manipulator.Runner;
using Manipulator.Scenarios.Specs;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using AnthropicRole = Anthropic.Models.Messages.Role;
using AnthropicTool = Anthropic.Models.Messages.Tool;

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
/// tool list to the LLM (via the official <c>Anthropic</c> SDK), executes tool calls through the
/// MCP client, and stops on finish / iteration limit / timeout / fatal error. Identical loop,
/// system prompt and model parameters are meant to be reused by the dsl/text approaches once they
/// exist (MAN-77, MAN-82).
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
                    Name = "manipulator-harness",
                },
                NullLoggerFactory.Instance
            );
            mcpClient = await McpClient.CreateAsync(
                transport,
                cancellationToken: cancellationToken
            );

            var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
            var tools = mcpTools
                .Select(tool =>
                    (ToolUnion)
                        new AnthropicTool
                        {
                            Name = tool.Name,
                            Description = tool.Description,
                            InputSchema = ToInputSchema(tool.JsonSchema),
                        }
                )
                .ToList();

            var promptText = ResolvePrompt(spec, config.Variant);
            var messages = new List<MessageParam>
            {
                new() { Role = AnthropicRole.User, Content = promptText },
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

                Message response;
                try
                {
                    response = await anthropicClient.Messages.Create(
                        new MessageCreateParams
                        {
                            Model = config.Model,
                            MaxTokens = HarnessConstants.MaxTokens,
                            System = HarnessConstants.SystemPrompt,
                            Messages = messages,
                            Tools = tools,
                        },
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
                inputTokens += response.Usage.InputTokens;
                outputTokens += response.Usage.OutputTokens;

                messages.Add(
                    new MessageParam
                    {
                        Role = AnthropicRole.Assistant,
                        Content = response
                            .Content.Select(block => new ContentBlockParam(block.Json))
                            .ToList(),
                    }
                );

                var toolUses = response
                    .Content.Select(block => block.TryPickToolUse(out var toolUse) ? toolUse : null)
                    .Where(toolUse => toolUse is not null)
                    .Select(toolUse => toolUse!)
                    .ToList();

                var stepCalls = new List<ToolCallLog>();

                if (toolUses.Count == 0)
                {
                    onStep?.Invoke(
                        new StepRecord(
                            runId,
                            llmCalls,
                            response.Usage.InputTokens,
                            response.Usage.OutputTokens,
                            response.StopReason?.ToString() ?? "end_turn",
                            stepCalls
                        )
                    );
                    stopReason = StopReason.ModelStoppedNaturally;
                    break;
                }

                var resultBlocks = new List<ContentBlockParam>();
                foreach (var toolUse in toolUses)
                {
                    var toolName = toolUse.Name;
                    var toolUseId = toolUse.ID;
                    var arguments = toolUse.Input.ToDictionary(
                        kv => kv.Key,
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
                            JsonSerializer.Serialize(toolUse.Input),
                            isError,
                            errorMessage
                        )
                    );

                    resultBlocks.Add(
                        new ToolResultBlockParam(toolUseId)
                        {
                            Content = resultText,
                            IsError = isError,
                        }
                    );
                }

                messages.Add(
                    new MessageParam { Role = AnthropicRole.User, Content = resultBlocks }
                );

                onStep?.Invoke(
                    new StepRecord(
                        runId,
                        llmCalls,
                        response.Usage.InputTokens,
                        response.Usage.OutputTokens,
                        response.StopReason?.ToString() ?? "tool_use",
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
            finalSceneJson
        );
    }

    private static InputSchema ToInputSchema(JsonElement mcpJsonSchema)
    {
        var properties = mcpJsonSchema.TryGetProperty("properties", out var props)
            ? props.EnumerateObject().ToDictionary(p => p.Name, p => p.Value)
            : new Dictionary<string, JsonElement>();
        var required = mcpJsonSchema.TryGetProperty("required", out var req)
            ? req.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [];

        return new InputSchema { Properties = properties, Required = required };
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
