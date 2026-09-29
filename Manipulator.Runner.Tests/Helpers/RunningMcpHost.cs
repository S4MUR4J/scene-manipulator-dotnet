using Manipulator.Core.Ecs;
using Manipulator.Runner.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace Manipulator.Runner.Tests.Helpers;

/// <summary>A started <see cref="ScenarioMcpHost"/> with a connected MCP client, as AgentLoop uses it.</summary>
sealed class RunningMcpHost(WebApplication app, McpClient client) : IAsyncDisposable
{
    public McpClient Client { get; } = client;

    public Scene Scene => app.Services.GetRequiredService<Scene>();

    public static async Task<RunningMcpHost> StartAsync(string approach, Scene? startingScene = null)
    {
        var app = ScenarioMcpHost.Build(startingScene, approach);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        var baseAddress = app
            .Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();
        var client = await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri($"{baseAddress}/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp,
                },
                NullLoggerFactory.Instance
            )
        );

        return new RunningMcpHost(app, client);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
