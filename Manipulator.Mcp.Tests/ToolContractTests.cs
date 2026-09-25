using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Mcp.Tests.Helpers;
using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp.Tests;

/// <summary>
/// The tool surface is a controlled constant of the experiment: names, descriptions and parameter
/// schemas have to be identical across every run, otherwise runs are not comparable. The snapshot
/// makes any change to them a deliberate, reviewable edit.
/// </summary>
public class ToolContractTests
{
    private static readonly string SnapshotPath = Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "tools.snapshot.json"
    );

    [Fact]
    public async Task Tool_names_descriptions_and_schemas_match_the_committed_snapshot()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();

        var tools = await client.ListToolsAsync();
        var contract = new JsonArray();
        foreach (var tool in tools.OrderBy(tool => tool.Name, StringComparer.Ordinal))
        {
            contract.Add(
                new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["input_schema"] = JsonNode.Parse(tool.JsonSchema.GetRawText()),
                }
            );
        }

        var actual = contract.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        // Regenerate with MANIPULATOR_UPDATE_SNAPSHOT=1 dotnet test, then review the diff: a change
        // here changes what every future run of the experiment shows the agent.
        if (Environment.GetEnvironmentVariable("MANIPULATOR_UPDATE_SNAPSHOT") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath)!);
            await File.WriteAllTextAsync(SnapshotPath, actual);
            Assert.Fail($"Snapshot rewritten to '{SnapshotPath}'; copy it into the project.");
        }

        var expected = await File.ReadAllTextAsync(SnapshotPath);

        // Assert.Equal, not FluentAssertions: the JSON braces in a failure message break its
        // string.Format-based formatter.
        Assert.Equal(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }

    [Fact]
    public void The_geometry_description_lists_every_geometry_type()
    {
        var documented = ToolDescriptions
            .Geometries.Split(',')
            .Select(name => name.Trim())
            .ToArray();

        documented.Should().Equal(Enum.GetNames<GeometryType>());
    }
}
