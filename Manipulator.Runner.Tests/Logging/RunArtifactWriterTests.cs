using System.Text.Json;
using FluentAssertions;
using Manipulator.Runner.Configuration;
using Manipulator.Runner.Logging;

namespace Manipulator.Runner.Tests.Logging;

public class RunArtifactWriterTests
{
    private static readonly DateTimeOffset BatchStartedAt = new(
        2026,
        9,
        28,
        14,
        30,
        5,
        TimeSpan.Zero
    );

    [Fact]
    public void Open_MultipleStepWrites_WritesOneJsonObjectPerLine()
    {
        var root = Path.Combine(Path.GetTempPath(), $"run-artifact-writer-test-{Guid.NewGuid():n}");
        var writer = new RunArtifactWriter(root);
        var config = CreateConfig("model");
        var runId = "run-1";

        try
        {
            using (var artifacts = writer.Open(config, runId, BatchStartedAt))
            {
                artifacts.WriteStep(new StepRecord("run-1", 1, 10, 20, "tool_use", []));
                artifacts.WriteStep(new StepRecord("run-1", 2, 5, 8, "end_turn", []));
            }

            var lines = File.ReadAllLines(writer.JsonlPath(config, runId, BatchStartedAt));

            lines.Should().HaveCount(2);
            foreach (var line in lines)
            {
                var node = JsonDocument.Parse(line).RootElement;
                node.GetProperty("Step").GetProperty("RunId").GetString().Should().Be("run-1");
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SaveFinalScene_WritesFileContentVerbatim()
    {
        var root = Path.Combine(Path.GetTempPath(), $"run-artifact-writer-test-{Guid.NewGuid():n}");
        var writer = new RunArtifactWriter(root);
        var config = CreateConfig("model");
        const string sceneJson = """{"version":"1.0","scene_version":0,"entities":[]}""";

        try
        {
            using var artifacts = writer.Open(config, "run-1", BatchStartedAt);
            artifacts.SaveFinalScene(sceneJson);

            File.ReadAllText(artifacts.FinalScenePath).Should().Be(sceneJson);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ArtifactPaths_UseModelNameToSeparateConcurrentRuns()
    {
        var sonnet = CreateConfig("claude-sonnet");
        var gpt = CreateConfig("gpt-5");

        var writer = new RunArtifactWriter("runs");

        writer
            .JsonlPath(sonnet, "run-1", BatchStartedAt)
            .Should()
            .NotBe(writer.JsonlPath(gpt, "run-1", BatchStartedAt));
        writer
            .FinalScenePath(sonnet, "run-1", BatchStartedAt)
            .Should()
            .NotBe(writer.FinalScenePath(gpt, "run-1", BatchStartedAt));
    }

    [Fact]
    public void ArtifactPaths_UseRunIdToSeparateRepeatedRuns()
    {
        var config = CreateConfig("claude-sonnet");
        var writer = new RunArtifactWriter("runs");

        writer
            .JsonlPath(config, "run-1", BatchStartedAt)
            .Should()
            .NotBe(writer.JsonlPath(config, "run-2", BatchStartedAt));
        writer
            .FinalScenePath(config, "run-1", BatchStartedAt)
            .Should()
            .NotBe(writer.FinalScenePath(config, "run-2", BatchStartedAt));
    }

    [Fact]
    public void ArtifactPaths_UseBatchStartedAtToSeparateRepeatedBatches()
    {
        var config = CreateConfig("claude-sonnet");
        var writer = new RunArtifactWriter("runs");
        var laterBatch = BatchStartedAt.AddDays(1);

        writer
            .JsonlPath(config, "run-1", BatchStartedAt)
            .Should()
            .NotBe(writer.JsonlPath(config, "run-1", laterBatch));
        writer
            .FinalScenePath(config, "run-1", BatchStartedAt)
            .Should()
            .NotBe(writer.FinalScenePath(config, "run-1", laterBatch));
    }

    [Fact]
    public void ArtifactPaths_NestBatchTimestampThenRunIdThenPlainFilename()
    {
        var config = CreateConfig("claude-sonnet");
        var writer = new RunArtifactWriter("runs");

        var jsonlPath = writer.JsonlPath(config, "run-1", BatchStartedAt);
        var finalScenePath = writer.FinalScenePath(config, "run-1", BatchStartedAt);

        var expectedRunDir = Path.Combine("runs", "batch_2026-09-28_14-30-05", "run-1");
        Path.GetDirectoryName(jsonlPath).Should().EndWith(expectedRunDir);
        Path.GetFileName(jsonlPath).Should().NotContain("run-1");
        Path.GetDirectoryName(finalScenePath).Should().EndWith(expectedRunDir);
        Path.GetFileName(finalScenePath).Should().NotContain("run-1");
    }

    private static RunConfig CreateConfig(string modelName) =>
        new RunConfig(
            Scenario: "1",
            Variant: null,
            Approach: "mcp",
            ModelName: modelName,
            Provider: ModelProvider.Anthropic,
            Model: "model",
            Seed: null,
            RunIndex: 0,
            ScenarioFile: "scenario.json",
            Batch: "batch",
            MaxToolIterations: 10,
            TimeoutSeconds: null
        );
}
