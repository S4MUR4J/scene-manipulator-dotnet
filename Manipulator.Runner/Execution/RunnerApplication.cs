using Manipulator.Runner.Logging;
using Manipulator.Runner.Models;
using Manipulator.Runner.Configuration;
using Manipulator.Scenarios.Loading;
using Microsoft.Extensions.Options;
using ScenarioSpec = Manipulator.Scenarios.Specs.ScenarioSpec;

namespace Manipulator.Runner.Execution;

sealed class RunnerApplication(
    IOptions<RunnerSettings> runnerSettings,
    IConfiguration configuration,
    ModelStrategyFactory strategyFactory
)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RunConfig> runConfigs;
        try
        {
            if (!configuration.GetSection("Runner").Exists())
                throw new ArgumentException("Runner configuration section is required.");

            runConfigs = runnerSettings.Value.ToRunConfigs();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Invalid Runner configuration: {ex.Message}");
            return 2;
        }

        var missingKeys = GetMissingApiKeys(runConfigs);
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

        var records = await Task.WhenAll(
            runConfigs.Select(config => RunModelAsync(config, spec, cancellationToken))
        );

        return records.All(record => record.StopReason != StopReason.FatalError.ToString()) ? 0 : 1;
    }

    private List<string> GetMissingApiKeys(IReadOnlyList<RunConfig> runConfigs)
    {
        var requiredProviders = runConfigs.Select(config => config.Provider).Distinct().ToHashSet();
        var missingKeys = new List<string>();

        if (
            requiredProviders.Contains(ModelProvider.Anthropic)
            && string.IsNullOrWhiteSpace(configuration["Anthropic:ApiKey"])
        )
            missingKeys.Add("Anthropic:ApiKey");
        if (
            requiredProviders.Contains(ModelProvider.OpenAi)
            && string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"])
        )
            missingKeys.Add("OpenAI:ApiKey");

        return missingKeys;
    }

    private async Task<RunRecord> RunModelAsync(
        RunConfig config,
        ScenarioSpec spec,
        CancellationToken cancellationToken
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
            cancellationToken
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
}
