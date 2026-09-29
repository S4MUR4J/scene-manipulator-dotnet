namespace Manipulator.Runner.Configuration;

public static class Approaches
{
    public const string Mcp = "mcp";
    public const string Text = "text";

    public static readonly IReadOnlySet<string> Implemented = new HashSet<string> { Mcp, Text };
}
