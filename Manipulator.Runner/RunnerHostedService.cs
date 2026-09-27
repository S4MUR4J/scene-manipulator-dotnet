namespace Manipulator.Runner;

sealed class RunnerHostedService(
    RunnerApplication application,
    RunnerExitCode exitCode,
    IHostApplicationLifetime lifetime
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
            await Console.Error.WriteLineAsync($"Runner failed unexpectedly: {ex.Message}");
            exitCode.Value = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
