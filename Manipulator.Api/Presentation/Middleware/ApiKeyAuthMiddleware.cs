namespace Manipulator.Api;

public class ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string ApiKeySectionName = "Authentication:ApiKey";
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var requestedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Missing API key.");
            return;
        }

        var apiKey = configuration.GetValue<string>(ApiKeySectionName);
        if (apiKey is null || requestedApiKey != apiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Invalid API key.");
            return;
        }

        await next(context);
    }
}
