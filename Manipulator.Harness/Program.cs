using Manipulator.Harness;
using Manipulator.Harness.Anthropic;
using Manipulator.Harness.Logging;
using Manipulator.Scenarios.Loading;
using Manipulator.Scenarios.Specs;
using RunConfig = Manipulator.Harness.RunConfig;

var parsedArgs = ParseArgs(args);

RunConfig config;
try
{
    config = RunConfig.FromArgs(parsedArgs);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Invalid run config: {ex.Message}");
    Console.Error.WriteLine(
        "Usage: dotnet run --project Manipulator.Harness -- "
            + "--scenario-file scenarios/s1-new-gen-livingroom.json "
            + "[--scenario id] [--variant n] [--approach mcp] [--model claude-sonnet-5] [--seed n] "
            + "[--run-index n] [--batch name] [--out-dir runs] [--max-iterations n] [--timeout-s n]"
    );
    return 2;
}

var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine("ANTHROPIC_API_KEY is not set.");
    return 2;
}

ScenarioSpec spec;
try
{
    spec = ScenarioSpecLoader.LoadFile(config.ScenarioFile);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to load scenario spec: {ex.Message}");
    return 2;
}

using var httpClient = new HttpClient();
var anthropicClient = new AnthropicClient(httpClient, apiKey);
var agentLoop = new AgentLoop(anthropicClient);

var jsonlPath = RunLogger.JsonlPath(config.OutDir, config.Batch);
var finalScenePath = RunLogger.FinalScenePath(config.OutDir, config.Batch, config);

Console.WriteLine($"Running scenario '{spec.Id}' ({config.Approach}, {config.Model})...");

var record = await agentLoop.RunAsync(
    config,
    spec,
    step => RunLogger.AppendRecord(jsonlPath, step),
    CancellationToken.None
);

RunLogger.AppendRecord(jsonlPath, record);
RunLogger.SaveFinalScene(finalScenePath, record.FinalScene!.ToJsonString());

Console.WriteLine(
    $"Done: stop_reason={record.StopReason} llm_calls={record.LlmCalls} "
        + $"tool_calls={record.ToolCallsTotal} tokens_in={record.InputTokens} tokens_out={record.OutputTokens}"
);
Console.WriteLine($"Run record: {jsonlPath}");
Console.WriteLine($"Final scene: {finalScenePath}");

return 0;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>();
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--"))
            continue;

        var key = args[i][2..];
        var value = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true";
        result[key] = value;
    }

    return result;
}
