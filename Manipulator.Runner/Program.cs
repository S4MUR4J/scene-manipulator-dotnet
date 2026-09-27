using System.Reflection;
using Anthropic;
using Manipulator.Runner;
using Manipulator.Runner.Logging;
using Manipulator.Scenarios.Loading;
using Manipulator.Scenarios.Specs;
using RunConfig = Manipulator.Runner.RunConfig;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

RunConfig config;
try
{
    config = RunConfig.FromConfiguration(configuration);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Invalid run config: {ex.Message}");
    Console.Error.WriteLine(
        "Set defaults in appsettings.json, or override per run, e.g.:\n"
            + "  dotnet run --project Manipulator.Runner -- "
            + "--scenario-file ../scenarios/s1-new-gen-livingroom.json "
            + "[--scenario id] [--variant n] [--approach mcp] [--model claude-sonnet-5] [--seed n] "
            + "[--run-index n] [--batch name] [--out-dir runs] [--max-iterations n] [--timeout-s n]"
    );
    return 2;
}

var apiKey = configuration["ANTHROPIC_API_KEY"];
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine(
        "ANTHROPIC_API_KEY is not set. Configure it via "
            + "'dotnet user-secrets set ANTHROPIC_API_KEY <key> --project Manipulator.Runner' "
            + "or the ANTHROPIC_API_KEY environment variable - never appsettings.json."
    );
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

var anthropicClient = new AnthropicClient { ApiKey = apiKey };
var agentLoop = new AgentLoop(anthropicClient);

var jsonlPath = RunLogger.JsonlPath(config.OutDir, config.Batch);
var finalScenePath = RunLogger.FinalScenePath(config.OutDir, config.Batch, config);
using var logger = RunLogger.CreateLogger(jsonlPath);

Console.WriteLine($"Running scenario '{spec.Id}' ({config.Approach}, {config.Model})...");

var record = await agentLoop.RunAsync(
    config,
    spec,
    step => logger.Information("step {@Step}", step),
    CancellationToken.None
);

logger.Information("run {@Run}", record);
RunLogger.SaveFinalScene(finalScenePath, record.FinalSceneJson);

Console.WriteLine(
    $"Done: stop_reason={record.StopReason} llm_calls={record.LlmCalls} "
        + $"tool_calls={record.ToolCallsTotal} tokens_in={record.InputTokens} tokens_out={record.OutputTokens}"
);
Console.WriteLine($"Run record: {jsonlPath}");
Console.WriteLine($"Final scene: {finalScenePath}");

return 0;
