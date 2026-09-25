using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Manipulator.Mcp.Tests.Helpers;

/// <summary>
/// Runs the real MCP server in-process and connects real MCP clients to it over the streamable HTTP
/// transport, so the tests exercise the same path the harness will.
/// </summary>
internal sealed class McpTestApp : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<McpClient> _clients = [];

    private McpTestApp(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public static McpTestApp Start(params (string Key, string? Value)[] settings)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
        });

        return new McpTestApp(factory);
    }

    public HttpClient CreateHttpClient()
    {
        return _factory.CreateClient();
    }

    public async Task<McpClient> ConnectAsync()
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri("http://localhost/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
                EnableStandaloneGetStream = false,
            },
            CreateHttpClient()
        );

        var client = await McpClient.CreateAsync(transport);
        _clients.Add(client);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
            await client.DisposeAsync();

        await _factory.DisposeAsync();
    }
}

internal static class McpClientExtensions
{
    /// <summary>Calls a tool and parses the JSON envelope every Manipulator tool returns.</summary>
    public static async Task<JsonObject> CallJsonAsync(
        this McpClient client,
        string tool,
        Dictionary<string, object?>? arguments = null
    )
    {
        var result = await client.CallToolAsync(
            tool,
            arguments ?? new Dictionary<string, object?>()
        );

        var text = string.Concat(
            result.Content.OfType<TextContentBlock>().Select(block => block.Text)
        );

        return JsonNode.Parse(text)?.AsObject()
            ?? throw new InvalidOperationException($"Tool '{tool}' returned no JSON: '{text}'.");
    }

    public static async Task<string> AddCubeAsync(this McpClient client, string? name = null)
    {
        var response = await client.CallJsonAsync(
            "add_entity",
            new Dictionary<string, object?> { ["geometry"] = "Cube", ["name"] = name }
        );

        return response["entity_id"]!.GetValue<string>();
    }
}
