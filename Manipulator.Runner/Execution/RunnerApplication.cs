using Manipulator.Core.Serialization;
using Manipulator.Runner.Configuration;
using Manipulator.Runner.Logging;
using Manipulator.Runner.Models;
using Manipulator.Scenarios.Loading;
using Manipulator.Scenarios.Scoring;
using Microsoft.Extensions.Options;
using ScenarioSpec = Manipulator.Scenarios.Specs.ScenarioSpec;

namespace Manipulator.Runner.Execution;

sealed class RunnerApplication(
    IOptions<RunnerSettings> runnerSettings,
    IConfiguration configuration,
    ModelStrategyFactory strategyFactory,
    RunArtifactWriter artifactWriter,
    ILogger<RunnerApplication> logger
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
            logger.LogError(ex, "Invalid Runner configuration");
            return 2;
        }

        var missingKeys = GetMissingApiKeys(runConfigs);
        if (missingKeys.Count > 0)
        {
            logger.LogError(
                "Missing required user-secret key(s): {MissingKeys}. Configure them with "
                    + "'dotnet user-secrets set <Provider>:ApiKey <key> --project Manipulator.Runner'.",
                string.Join(", ", missingKeys)
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
            logger.LogError(ex, "Failed to load scenario spec");
            return 2;
        }

        var batchStartedAt = DateTimeOffset.UtcNow;
        var records = await Task.WhenAll(
            runConfigs.Select(config =>
                RunModelAsync(config, spec, batchStartedAt, cancellationToken)
            )
        );

        return records.All(record => record.StopReason != nameof(StopReason.FatalError)) ? 0 : 1;
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
        DateTimeOffset batchStartedAt,
        CancellationToken cancellationToken
    )
    {
        var runId = Guid.NewGuid().ToString("n");
        using var artifacts = artifactWriter.Open(config, runId, batchStartedAt);
        var agentLoop = new AgentLoop(strategyFactory.Create(config));

        logger.LogInformation(
            "Running {ModelName} for scenario {ScenarioId} ({Approach}, {Model}) runId={RunId}",
            config.ModelName,
            spec.Id,
            config.Approach,
            config.Model,
            runId
        );

        var record = await agentLoop.RunAsync(
            config: config,
            spec: spec,
            runId: runId,
            onStep: artifacts.WriteStep,
            cancellationToken: cancellationToken
        );

        var finalScene = SceneSerializer.Deserialize(record.FinalSceneJson).Scene;
        var scoring = SceneScorer.Score(spec, finalScene);
        record = record with { Scoring = ScoringLog.From(scoring) };

        artifacts.WriteRun(record);
        artifacts.SaveFinalScene(record.FinalSceneJson);

        logger.LogInformation(
            "Completed {ModelName}: stop_reason={StopReason} llm_calls={LlmCalls} tool_calls={ToolCalls} "
                + "tokens_in={InputTokens} tokens_out={OutputTokens} coverage={Coverage} success={Success} runId={RunId}",
            config.ModelName,
            record.StopReason,
            record.LlmCalls,
            record.ToolCallsTotal,
            record.InputTokens,
            record.OutputTokens,
            scoring.Coverage,
            scoring.Success,
            runId
        );
        logger.LogInformation(
            "Run artifacts for {ModelName}: record={RecordPath} final_scene={FinalScenePath} runId={RunId}",
            config.ModelName,
            artifacts.JsonlPath,
            artifacts.FinalScenePath,
            runId
        );

        return record;
    }
}
