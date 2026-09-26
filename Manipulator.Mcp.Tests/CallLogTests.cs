using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Manipulator.Mcp.Tests.Helpers;

namespace Manipulator.Mcp.Tests;

public class CallLogTests
{
    [Fact]
    public async Task Every_call_read_write_and_error_is_recorded_for_the_harness()
    {
        var logPath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-log-{Guid.NewGuid():N}",
            "run.jsonl"
        );

        await using var app = McpTestApp.Start(("call-log", logPath));
        var client = await app.ConnectAsync();

        var entityId = await client.AddCubeAsync("box");
        await client.CallJsonAsync("get_scene");
        await client.CallJsonAsync(
            "move_entity",
            new Dictionary<string, object?>
            {
                ["entityId"] = "missing",
                ["position"] = new[] { 0f, 1f, 0f },
            }
        );
        await client.CallJsonAsync("finish");

        var entries = (await File.ReadAllLinesAsync(logPath))
            .Select(line => JsonNode.Parse(line)!.AsObject())
            .ToList();

        entries
            .Select(entry => entry["name"]!.GetValue<string>())
            .Should()
            .ContainInOrder(
                "run_started",
                "EntityAddedEvent",
                "add_entity",
                "get_scene",
                "move_entity",
                "run_finished",
                "finish"
            );

        var add = entries.Single(entry => entry["name"]!.GetValue<string>() == "add_entity");
        add["kind"]!.GetValue<string>().Should().Be("tool_call");
        add["ok"]!.GetValue<bool>().Should().BeTrue();
        add["arguments"]!["geometry"]!.GetValue<string>().Should().Be("Cube");
        add["scene_version_before"]!.GetValue<long>().Should().Be(0);
        add["scene_version_after"]!.GetValue<long>().Should().Be(1);
        add["data"]!["entity_id"]!.GetValue<string>().Should().Be(entityId);

        var read = entries.Single(entry => entry["name"]!.GetValue<string>() == "get_scene");
        read["kind"]!.GetValue<string>().Should().Be("tool_call");
        read["scene_version_before"]!.GetValue<long>().Should().Be(1);
        read["scene_version_after"]!.GetValue<long>().Should().Be(1);

        var failed = entries.Single(entry => entry["name"]!.GetValue<string>() == "move_entity");
        failed["ok"]!.GetValue<bool>().Should().BeFalse();
        failed["error"]!.GetValue<string>().Should().Be("Entity 'missing' does not exist.");

        var added = entries.Single(entry => entry["name"]!.GetValue<string>() == "EntityAddedEvent");
        added["kind"]!.GetValue<string>().Should().Be("event");
        added["data"]!["entity_id"]!.GetValue<string>().Should().Be(entityId);
        added["call_index"]!.GetValue<long>().Should().Be(1);

        var finished = entries.Single(entry =>
            entry["name"]!.GetValue<string>() == "run_finished"
        );
        finished["data"]!["scene"]!["entities"]!.AsArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task The_runner_can_probe_the_server_before_it_connects_an_agent()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();
        await client.AddCubeAsync();

        var health = JsonNode.Parse(await app.CreateHttpClient().GetStringAsync("/health"))!;

        health["run_id"]!.GetValue<string>().Should().Be("run");
        health["entity_count"]!.GetValue<int>().Should().Be(1);
        health["finished"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task The_log_is_valid_jsonl()
    {
        var logPath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-log-{Guid.NewGuid():N}",
            "run.jsonl"
        );

        await using var app = McpTestApp.Start(("call-log", logPath));
        var client = await app.ConnectAsync();
        await client.AddCubeAsync();

        foreach (var line in await File.ReadAllLinesAsync(logPath))
        {
            var parse = () => JsonDocument.Parse(line);
            parse.Should().NotThrow();
        }
    }
}
