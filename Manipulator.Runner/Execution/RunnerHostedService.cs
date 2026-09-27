namespace Manipulator.Runner.Execution;

sealed class RunnerHostedService(
    RunnerApplication application,
    RunnerExitCode exitCode,
    IHostApplicationLifetime lifetime,
    ILogger<RunnerHostedService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            exitCode.Value = await application.RunAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Runner failed unexpectedly");
            exitCode.Value = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
