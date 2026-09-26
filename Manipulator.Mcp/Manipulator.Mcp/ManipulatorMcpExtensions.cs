using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp;

public static class ManipulatorMcpExtensions
{
    public static IServiceCollection AddManipulatorMcp(this IServiceCollection services)
    {
        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<SceneReadTools>()
            .WithTools<SceneWriteTools>();

        return services;
    }

    public static IEndpointRouteBuilder MapManipulatorMcp(this IEndpointRouteBuilder app)
    {
        app.MapMcp("/mcp");

        return app;
    }
}
