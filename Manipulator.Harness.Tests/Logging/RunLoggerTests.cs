using System.Text.Json;
using FluentAssertions;
using Manipulator.Harness.Logging;

namespace Manipulator.Harness.Tests.Logging;

public class RunLoggerTests
{
    [Fact]
    public void AppendRecord_MultipleCalls_WritesOneJsonObjectPerLine()
    {
        var path = Path.Combine(Path.GetTempPath(), $"run-logger-test-{Guid.NewGuid():n}.jsonl");

        try
        {
            RunLogger.AppendRecord(path, new StepRecord("run-1", 1, 10, 20, "tool_use", []));
            RunLogger.AppendRecord(path, new StepRecord("run-1", 2, 5, 8, "end_turn", []));

            var lines = File.ReadAllLines(path);

            lines.Should().HaveCount(2);
            foreach (var line in lines)
            {
                var node = JsonDocument.Parse(line).RootElement;
                node.GetProperty("record_type").GetString().Should().Be("step");
                node.GetProperty("run_id").GetString().Should().Be("run-1");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveFinalScene_WritesFileContentVerbatim()
    {
        var path = Path.Combine(Path.GetTempPath(), $"final-scene-test-{Guid.NewGuid():n}.json");
        const string sceneJson = """{"version":"1.0","scene_version":0,"entities":[]}""";

        try
        {
            RunLogger.SaveFinalScene(path, sceneJson);

            File.ReadAllText(path).Should().Be(sceneJson);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
