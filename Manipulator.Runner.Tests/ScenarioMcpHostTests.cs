using FluentAssertions;
using Manipulator.Core.Ecs;
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
}
