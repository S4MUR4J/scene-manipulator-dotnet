using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp;

public static class ManipulatorMcpExtensions
{
    public static IMcpServerBuilder AddManipulatorMcp(this IServiceCollection services)
    {
        return services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<SceneReadTools>()
            .WithTools<SceneWriteTools>();
    }

    public static IEndpointRouteBuilder MapManipulatorMcp(this IEndpointRouteBuilder app)
    {
        app.MapMcp("/mcp");

        return app;
    }
}
