using System.Text.Json.Nodes;
using FluentAssertions;
using Manipulator.Mcp.Tests.Helpers;

namespace Manipulator.Mcp.Tests;

public class McpToolsTests
{
    private static readonly string[] ExpectedTools =
    [
        "add_entity",
        "move_entity",
        "rotate_entity",
        "scale_entity",
        "set_material",
        "rename_entity",
        "remove_entity",
        "get_scene",
        "get_entity",
        "finish",
    ];

    [Fact]
    public async Task Server_exposes_exactly_the_mcp_mode_tool_set()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();

        var tools = await client.ListToolsAsync();

        tools.Select(tool => tool.Name).Should().BeEquivalentTo(ExpectedTools);
    }

    [Fact]
    public async Task Add_entity_returns_an_id_that_get_scene_reports()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();

        var added = await client.CallJsonAsync(
            "add_entity",
            new Dictionary<string, object?>
            {
                ["geometry"] = "Sphere",
                ["position"] = new[] { 1f, 2f, 3f },
                ["color"] = "#ff0000",
                ["name"] = "sun",
            }
        );

        added["ok"]!.GetValue<bool>().Should().BeTrue();
        var entityId = added["entity_id"]!.GetValue<string>();

        var scene = await client.CallJsonAsync("get_scene");
        var entities = scene["scene"]!["entities"]!.AsArray();

        entities.Should().HaveCount(1);
        var entity = entities[0]!.AsObject();
        entity["id"]!.GetValue<string>().Should().Be(entityId);
        entity["components"]!["mesh_filter"]!["geometry"]!.GetValue<string>().Should().Be("Sphere");
        entity["components"]!["transform"]!["position"]!.AsArray()
            .Select(value => value!.GetValue<float>())
            .Should()
            .Equal(1f, 2f, 3f);
        entity["components"]!["entity_name"]!["value"]!.GetValue<string>().Should().Be("sun");
    }

    [Fact]
    public async Task Get_entity_returns_the_same_shape_as_get_scene()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();
        var entityId = await client.AddCubeAsync("table");

        var single = await client.CallJsonAsync(
            "get_entity",
            new Dictionary<string, object?> { ["entityId"] = entityId }
        );
        var scene = await client.CallJsonAsync("get_scene");

        var fromScene = scene["scene"]!["entities"]!.AsArray()[0]!;
        single["entity"]!.ToJsonString().Should().Be(fromScene.ToJsonString());
    }

    [Fact]
    public async Task Every_write_tool_changes_the_scene_and_bumps_the_version()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();
        var entityId = await client.AddCubeAsync();

        await client.CallJsonAsync(
            "move_entity",
            new Dictionary<string, object?>
            {
                ["entityId"] = entityId,
                ["position"] = new[] { 0f, 5f, 0f },
            }
        );
        await client.CallJsonAsync(
            "rotate_entity",
            new Dictionary<string, object?>
            {
                ["entityId"] = entityId,
                ["rotation"] = new[] { 0f, 90f, 0f },
            }
        );
        await client.CallJsonAsync(
            "scale_entity",
            new Dictionary<string, object?>
            {
                ["entityId"] = entityId,
                ["scale"] = new[] { 2f, 2f, 2f },
            }
        );
        await client.CallJsonAsync(
            "set_material",
            new Dictionary<string, object?>
            {
                ["entityId"] = entityId,
                ["color"] = "#00ff00",
                ["roughness"] = 0.2f,
            }
        );
        var renamed = await client.CallJsonAsync(
            "rename_entity",
            new Dictionary<string, object?> { ["entityId"] = entityId, ["name"] = "hero cube" }
        );

        renamed["ok"]!.GetValue<bool>().Should().BeTrue();

        var entity = (await client.CallJsonAsync(
            "get_entity",
            new Dictionary<string, object?> { ["entityId"] = entityId }
        ))["entity"]!;

        var transform = entity["components"]!["transform"]!;
        transform["position"]!.AsArray().Select(v => v!.GetValue<float>()).Should().Equal(0f, 5f, 0f);
        transform["rotation"]!.AsArray().Select(v => v!.GetValue<float>()).Should().Equal(0f, 90f, 0f);
        transform["scale"]!.AsArray().Select(v => v!.GetValue<float>()).Should().Equal(2f, 2f, 2f);
        entity["components"]!["mesh_renderer"]!["color"]!.GetValue<string>().Should().Be("#00ff00");
        entity["components"]!["mesh_renderer"]!["roughness"]!.GetValue<float>().Should().Be(0.2f);
        entity["components"]!["entity_name"]!["value"]!.GetValue<string>().Should().Be("hero cube");

        var scene = await client.CallJsonAsync("get_scene");
        scene["scene"]!["scene_version"]!.GetValue<long>().Should().Be(6);
    }

    [Fact]
    public async Task Remove_entity_deletes_it()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();
        var entityId = await client.AddCubeAsync();

        var removed = await client.CallJsonAsync(
            "remove_entity",
            new Dictionary<string, object?> { ["entityId"] = entityId }
        );

        removed["ok"]!.GetValue<bool>().Should().BeTrue();
        var scene = await client.CallJsonAsync("get_scene");
        scene["scene"]!["entities"]!.AsArray().Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(BadCalls))]
    public async Task Bad_input_comes_back_as_a_readable_error_instead_of_an_exception(
        string tool,
        Dictionary<string, object?> arguments,
        string expectedFragment
    )
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();

        var response = await client.CallJsonAsync(tool, arguments);

        response["ok"]!.GetValue<bool>().Should().BeFalse();
        response["error"]!.GetValue<string>().Should().Contain(expectedFragment);
    }

    public static TheoryData<string, Dictionary<string, object?>, string> BadCalls()
    {
        return new TheoryData<string, Dictionary<string, object?>, string>
        {
            {
                "add_entity",
                new Dictionary<string, object?> { ["geometry"] = "Pyramid" },
                "Unknown geometry 'Pyramid'. Valid values: Cube, Sphere"
            },
            {
                "add_entity",
                new Dictionary<string, object?>
                {
                    ["geometry"] = "Cube",
                    ["position"] = new[] { 1f, 2f },
                },
                "'position' must be an array of exactly 3 numbers"
            },
            {
                "add_entity",
                new Dictionary<string, object?>
                {
                    ["geometry"] = "Cube",
                    ["scale"] = new[] { 0f, 1f, 1f },
                },
                "Scale must be positive on every axis"
            },
            {
                "add_entity",
                new Dictionary<string, object?> { ["geometry"] = "Cube", ["color"] = "red" },
                "Color"
            },
            {
                "move_entity",
                new Dictionary<string, object?>
                {
                    ["entityId"] = "missing",
                    ["position"] = new[] { 0f, 0f, 0f },
                },
                "Entity 'missing' does not exist."
            },
            {
                "set_material",
                new Dictionary<string, object?>
                {
                    ["entityId"] = "missing",
                    ["opacity"] = 1.5f,
                },
                "Entity 'missing' does not exist."
            },
            {
                "get_entity",
                new Dictionary<string, object?> { ["entityId"] = "missing" },
                "Entity 'missing' does not exist."
            },
        };
    }

    [Fact]
    public async Task Material_validation_errors_reach_the_agent()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();
        var entityId = await client.AddCubeAsync();

        var response = await client.CallJsonAsync(
            "set_material",
            new Dictionary<string, object?> { ["entityId"] = entityId, ["opacity"] = 4f }
        );

        response["ok"]!.GetValue<bool>().Should().BeFalse();
        response["error"]!.GetValue<string>().Should().Contain("Opacity");
    }

    [Fact]
    public async Task Results_reach_the_agent_as_plain_readable_json()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();

        var result = await client.CallToolAsync(
            "add_entity",
            new Dictionary<string, object?> { ["geometry"] = "Pyramid" }
        );

        var text = string.Concat(
            result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>()
                .Select(block => block.Text)
        );

        // Escaped punctuation would cost tokens on every single call and make errors harder to read.
        text.Should().Contain("Unknown geometry 'Pyramid'");
        text.Should().NotContain("\\u00");
    }

    [Fact]
    public async Task Finish_summarises_the_run_and_closes_it()
    {
        await using var app = McpTestApp.Start();
        var client = await app.ConnectAsync();
        await client.AddCubeAsync();
        await client.CallJsonAsync(
            "move_entity",
            new Dictionary<string, object?>
            {
                ["entityId"] = "missing",
                ["position"] = new[] { 0f, 0f, 0f },
            }
        );

        var finished = await client.CallJsonAsync("finish");

        finished["ok"]!.GetValue<bool>().Should().BeTrue();
        finished["entity_count"]!.GetValue<int>().Should().Be(1);
        finished["tool_calls"]!.GetValue<int>().Should().Be(3);
        finished["failed_tool_calls"]!.GetValue<int>().Should().Be(1);
        finished["scene_events"]!.GetValue<int>().Should().Be(1);

        var afterFinish = await client.CallJsonAsync("get_scene");
        afterFinish["ok"]!.GetValue<bool>().Should().BeFalse();
        afterFinish["error"]!.GetValue<string>().Should().Contain("already finished");
    }
}
