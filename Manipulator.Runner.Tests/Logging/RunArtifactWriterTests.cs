using System.Text.Json;
using FluentAssertions;
using Manipulator.Runner.Configuration;
using Manipulator.Runner.Logging;

namespace Manipulator.Runner.Tests.Logging;

public class RunArtifactWriterTests
{
    [Fact]
    public void Open_MultipleStepWrites_WritesOneJsonObjectPerLine()
    {
        var outDir = Path.Combine(Path.GetTempPath(), $"run-artifact-writer-test-{Guid.NewGuid():n}");
        var writer = new RunArtifactWriter();
        var config = CreateConfig("model") with { OutDir = outDir };

        try
        {
            using (var artifacts = writer.Open(config))
            {
                artifacts.WriteStep(new StepRecord("run-1", 1, 10, 20, "tool_use", []));
                artifacts.WriteStep(new StepRecord("run-1", 2, 5, 8, "end_turn", []));
            }

            var lines = File.ReadAllLines(writer.JsonlPath(outDir, config.Batch, config));

            lines.Should().HaveCount(2);
            foreach (var line in lines)
            {
                var node = JsonDocument.Parse(line).RootElement;
                node.GetProperty("Step").GetProperty("RunId").GetString().Should().Be("run-1");
            }
        }
        finally
        {
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public void SaveFinalScene_WritesFileContentVerbatim()
    {
        var outDir = Path.Combine(Path.GetTempPath(), $"run-artifact-writer-test-{Guid.NewGuid():n}");
        var writer = new RunArtifactWriter();
        var config = CreateConfig("model") with { OutDir = outDir };
        const string sceneJson = """{"version":"1.0","scene_version":0,"entities":[]}""";

        try
        {
            using var artifacts = writer.Open(config);
            artifacts.SaveFinalScene(sceneJson);

            File.ReadAllText(artifacts.FinalScenePath).Should().Be(sceneJson);
        }
        finally
        {
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public void ArtifactPaths_UseModelNameToSeparateConcurrentRuns()
    {
        var sonnet = CreateConfig("claude-sonnet");
        var gpt = CreateConfig("gpt-5");

        var writer = new RunArtifactWriter();

        writer.JsonlPath("runs", "batch", sonnet)
            .Should()
            .NotBe(writer.JsonlPath("runs", "batch", gpt));
        writer.FinalScenePath("runs", "batch", sonnet)
            .Should()
            .NotBe(writer.FinalScenePath("runs", "batch", gpt));
    }

    private static RunConfig CreateConfig(string modelName) =>
        new(
            "1",
            null,
            "mcp",
            modelName,
            ModelProvider.Anthropic,
            "model",
            null,
            0,
            "scenario.json",
            "batch",
            "runs",
            10,
            null
        );
}
