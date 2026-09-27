namespace Manipulator.Scenarios.Loading;

public class ScenarioSpecException : Exception
{
    public ScenarioSpecException(string message)
        : base(message) { }

    public ScenarioSpecException(string message, Exception inner)
        : base(message, inner) { }
}
