using System.Reflection;
using Manipulator.Runner;
using Manipulator.Runner.Logging;
using Manipulator.Runner.Models;
using Manipulator.Scenarios.Loading;
using ScenarioSpec = Manipulator.Scenarios.Specs.ScenarioSpec;
using RunnerRunConfig = Manipulator.Runner.RunConfig;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.ScenarioOne.json", optional: false, reloadOnChange: false)
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .Build();

IReadOnlyList<RunnerRunConfig> runConfigs;
try
{
    var settings = configuration.GetRequiredSection("Runner").Get<RunnerSettings>()
        ?? throw new ArgumentException("Runner configuration section is required.");
    runConfigs = settings.ToRunConfigs();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Invalid Runner configuration: {ex.Message}");
    return 2;
}

var anthropicApiKey = configuration["Anthropic:ApiKey"];
var openAiApiKey = configuration["OpenAI:ApiKey"];
var requiredProviders = runConfigs.Select(config => config.Provider).Distinct().ToHashSet();
var missingKeys = new List<string>();
if (requiredProviders.Contains(ModelProvider.Anthropic) && string.IsNullOrWhiteSpace(anthropicApiKey))
    missingKeys.Add("Anthropic:ApiKey");
if (requiredProviders.Contains(ModelProvider.OpenAi) && string.IsNullOrWhiteSpace(openAiApiKey))
    missingKeys.Add("OpenAI:ApiKey");

if (missingKeys.Count > 0)
{
    Console.Error.WriteLine(
        $"Missing required user-secret key(s): {string.Join(", ", missingKeys)}. Configure them with:\n"
            + "  dotnet user-secrets set Anthropic:ApiKey <key> --project Manipulator.Runner\n"
            + "  dotnet user-secrets set OpenAI:ApiKey <key> --project Manipulator.Runner"
    );
    return 2;
}

ScenarioSpec spec;
try
{
    spec = ScenarioSpecLoader.LoadFile(runConfigs[0].ScenarioFile);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to load scenario spec: {ex.Message}");
    return 2;
}

var strategyFactory = new ModelStrategyFactory(anthropicApiKey!, openAiApiKey!);
var records = await Task.WhenAll(
    runConfigs.Select(config => RunModelAsync(config, spec, strategyFactory))
);

return records.All(record => record.StopReason != StopReason.FatalError.ToString()) ? 0 : 1;

static async Task<RunRecord> RunModelAsync(
    RunnerRunConfig config,
    ScenarioSpec spec,
    ModelStrategyFactory strategyFactory
)
{
    var jsonlPath = RunLogger.JsonlPath(config.OutDir, config.Batch, config);
    var finalScenePath = RunLogger.FinalScenePath(config.OutDir, config.Batch, config);
    using var logger = RunLogger.CreateLogger(jsonlPath);
    var agentLoop = new AgentLoop(strategyFactory.Create(config));

    Console.WriteLine(
        $"Running '{config.ModelName}' for scenario '{spec.Id}' ({config.Approach}, {config.Model})..."
    );

    var record = await agentLoop.RunAsync(
        config,
        spec,
        step => logger.Information("step {@Step}", step),
        CancellationToken.None
    );

    logger.Information("run {@Run}", record);
    RunLogger.SaveFinalScene(finalScenePath, record.FinalSceneJson);

    Console.WriteLine(
        $"Done ({config.ModelName}): stop_reason={record.StopReason} llm_calls={record.LlmCalls} "
            + $"tool_calls={record.ToolCallsTotal} tokens_in={record.InputTokens} "
            + $"tokens_out={record.OutputTokens}"
    );
    Console.WriteLine($"Run record ({config.ModelName}): {jsonlPath}");
    Console.WriteLine($"Final scene ({config.ModelName}): {finalScenePath}");

    return record;
}
