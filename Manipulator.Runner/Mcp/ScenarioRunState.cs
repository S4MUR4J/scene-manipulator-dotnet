namespace Manipulator.Runner.Mcp;

sealed class ScenarioRunState
{
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public bool IsFinished { get; private set; }

    public void MarkFinished()
    {
        IsFinished = true;
    }
}
