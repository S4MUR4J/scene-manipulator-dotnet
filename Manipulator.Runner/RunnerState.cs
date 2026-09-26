namespace Manipulator.Runner;

public sealed class RunnerState
{
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public bool IsFinished { get; private set; }

    public void MarkFinished()
    {
        IsFinished = true;
    }
}
