using FluentAssertions;
using Manipulator.Mcp.Tests.Helpers;

namespace Manipulator.Mcp.Tests;

public class RunTests
{
    [Fact]
    public async Task One_server_process_is_one_run_so_every_client_sees_the_same_scene()
    {
        await using var app = McpTestApp.Start();
        var first = await app.ConnectAsync();
        var second = await app.ConnectAsync();

        await first.AddCubeAsync("built by the first client");

        var secondScene = await second.CallJsonAsync("get_scene");

        secondScene["scene"]!["entities"]!.AsArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task Starting_scene_is_loaded_before_the_agent_connects()
    {
        var path = await WriteStartingSceneAsync();
        await using var app = McpTestApp.Start(("starting-scene", path));
        var client = await app.ConnectAsync();

        var scene = await client.CallJsonAsync("get_scene");

        var entities = scene["scene"]!["entities"]!.AsArray();
        entities.Should().HaveCount(1);
        entities[0]!["id"]!.GetValue<string>().Should().Be("floor");
        scene["scene"]!["scene_version"]!.GetValue<long>().Should().Be(7);
    }

    [Fact]
    public async Task A_starting_scene_can_be_modified_like_any_other()
    {
        var path = await WriteStartingSceneAsync();
        await using var app = McpTestApp.Start(("starting-scene", path));
        var client = await app.ConnectAsync();

        var moved = await client.CallJsonAsync(
            "move_entity",
            new Dictionary<string, object?>
            {
                ["entityId"] = "floor",
                ["position"] = new[] { 0f, -1f, 0f },
            }
        );

        moved["ok"]!.GetValue<bool>().Should().BeTrue();
        moved["scene_version"]!.GetValue<long>().Should().Be(8);
    }

    [Fact]
    public async Task A_broken_starting_scene_stops_the_server_before_any_agent_connects()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcp-broken-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, """{ "scene_version": 0, "entities": [ { } ] }""");

        await using var app = McpTestApp.Start(("starting-scene", path));

        var start = () => app.CreateHttpClient();

        start.Should().Throw<InvalidOperationException>().WithMessage("*is not a valid scene*");
    }

    [Fact]
    public async Task A_missing_starting_scene_file_stops_the_server()
    {
        await using var app = McpTestApp.Start(("starting-scene", "/does/not/exist.json"));

        var start = () => app.CreateHttpClient();

        start.Should().Throw<InvalidOperationException>().WithMessage("*does not exist*");
    }

    [Fact]
    public async Task An_unsupported_mode_stops_the_server()
    {
        await using var app = McpTestApp.Start(("mode", "dsl"));

        var start = () => app.CreateHttpClient();

        start.Should().Throw<InvalidOperationException>().WithMessage("*Unsupported mode 'dsl'*");
    }

    [Fact]
    public async Task The_run_id_tags_the_run_and_its_log()
    {
        await using var app = McpTestApp.Start(("run-id", "scenario-3-mcp-opus-7"));
        var client = await app.ConnectAsync();
        await client.AddCubeAsync();

        var health = System.Text.Json.Nodes.JsonNode.Parse(
            await app.CreateHttpClient().GetStringAsync("/health")
        )!;

        health["run_id"]!.GetValue<string>().Should().Be("scenario-3-mcp-opus-7");
    }

    private static async Task<string> WriteStartingSceneAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcp-start-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "version": "1.0",
              "scene_version": 7,
              "entities": [
                {
                  "id": "floor",
                  "components": {
                    "transform": { "position": [0, 0, 0], "rotation": [0, 0, 0], "scale": [10, 1, 10] },
                    "mesh_filter": { "geometry": "Plane" },
                    "mesh_renderer": { "color": "#cccccc", "opacity": 1, "metalness": 0, "roughness": 0.8 },
                    "entity_name": { "value": "floor" }
                  }
                }
              ]
            }
            """
        );

        return path;
    }
}
