using FluentAssertions;
using Manipulator.Runner.Configuration;
using Manipulator.Runner.Execution;
using Manipulator.Runner.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Manipulator.Runner.Tests;

public class RunnerApplicationTests
{
    [Fact]
    public async Task RunAsync_InvalidSettings_ReturnsUsageExitCode()
    {
        var configuration = new ConfigurationBuilder().Build();
        var application = CreateApplication(new RunnerSettings(), configuration);

        var exitCode = await application.RunAsync(CancellationToken.None);

        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_MissingRequiredProviderKey_ReturnsUsageExitCode()
    {
        var configuration = new ConfigurationBuilder().Build();
        var settings = new RunnerSettings
        {
            ScenarioFile = "not-loaded-before-key-validation.json",
            Models =
            [
                new ModelSettings
                {
                    Name = "claude",
                    Provider = "Anthropic",
                    Model = "claude-sonnet-5",
                },
            ],
        };
        var application = CreateApplication(settings, configuration);

        var exitCode = await application.RunAsync(CancellationToken.None);

        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task HostedService_InvalidSettings_RecordsUsageExitCode()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<RunnerSettings>(_ => { });
        builder.Services.AddSingleton<ModelStrategyFactory>();
        builder.Services.AddSingleton<RunnerApplication>();
        builder.Services.AddSingleton<RunnerExitCode>();
        builder.Services.AddHostedService<RunnerHostedService>();
        using var host = builder.Build();
        var exitCode = host.Services.GetRequiredService<RunnerExitCode>();

        await host.RunAsync();

        exitCode.Value.Should().Be(2);
    }

    private static RunnerApplication CreateApplication(
        RunnerSettings settings,
        IConfiguration configuration
    ) => new(Options.Create(settings), configuration, new ModelStrategyFactory(configuration));
}
