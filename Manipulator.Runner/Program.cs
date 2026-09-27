using Manipulator.Runner;
using Manipulator.Runner.Models;

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory,
    }
);
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.ScenarioOne.json", optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args);
builder.Logging.ClearProviders();

builder.Services.Configure<RunnerSettings>(builder.Configuration.GetSection("Runner"));
builder.Services.AddSingleton<ModelStrategyFactory>();
builder.Services.AddSingleton<RunnerApplication>();
builder.Services.AddSingleton<RunnerExitCode>();
builder.Services.AddHostedService<RunnerHostedService>();

using var host = builder.Build();
var exitCode = host.Services.GetRequiredService<RunnerExitCode>();
await host.RunAsync();
return exitCode.Value;
