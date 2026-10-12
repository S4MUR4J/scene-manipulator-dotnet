using FluentAssertions;
using Manipulator.Core.Ecs;
using Manipulator.Runner.Configuration;
using Manipulator.Runner.Mcp;
using Manipulator.Runner.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Manipulator.Runner.Tests;

public class ScenarioMcpHostTests
{
    [Fact]
    public async Task Build_WithStartingScene_RegistersAnIsolatedSceneAndRunState()
    {
        var startingScene = new Scene();
        var app = ScenarioMcpHost.Build(startingScene);

        try
        {
            app.Services.GetRequiredService<Scene>().Should().NotBeSameAs(startingScene);
            app.Services.GetRequiredService<ScenarioRunState>().IsFinished.Should().BeFalse();
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Build_TextApproach_ExposesOnlyGetSceneSubmitSceneAndFinish()
    {
        var toolNames = await ListToolNamesAsync(Approaches.Text);

        toolNames.Should().BeEquivalentTo("get_scene", "submit_scene", "finish");
    }

    [Fact]
    public async Task Build_McpApproach_DoesNotExposeSubmitScene()
    {
        var toolNames = await ListToolNamesAsync(Approaches.Mcp);

        toolNames.Should().Contain(["get_scene", "add_entity", "finish"]);
        toolNames.Should().NotContain("submit_scene");
    }

    private static async Task<IReadOnlyList<string>> ListToolNamesAsync(string approach)
    {
        await using var host = await RunningMcpHost.StartAsync(approach);
        var tools = await host.Client.ListToolsAsync();
        return tools.Select(tool => tool.Name).ToList();
    }
}
