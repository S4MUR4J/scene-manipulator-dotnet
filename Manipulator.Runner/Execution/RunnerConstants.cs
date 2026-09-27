namespace Manipulator.Runner.Execution;

/// <summary>
/// Controlled constants for the agent loop. These must stay identical across the mcp/dsl/text
/// approaches so a comparison between them isn't confounded by loop or prompting differences.
/// </summary>
public static class RunnerConstants
{
    public const string SystemPrompt =
        "You control a 3D scene made of entities. Each entity has a transform (position, "
        + "rotation, scale), a geometry and a material. You act on the scene only through the "
        + "tools you are given; you cannot see a render of the scene, only the JSON state "
        + "returned by the tools. Read the scene before deciding what to do if you are not sure "
        + "of its current state. Work through the user's request completely, then call the "
        + "finish tool exactly once when the scene satisfies it and there is nothing left to do. "
        + "Do not describe what you would do instead of doing it - call the tools.";

    public const long MaxTokens = 4096;

    public const int DefaultMaxToolIterations = 30;
    public const int DefaultTimeoutSeconds = 300;
}
