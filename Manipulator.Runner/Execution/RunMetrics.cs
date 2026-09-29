using Manipulator.Runner.Logging;
using Manipulator.Runner.Models;

namespace Manipulator.Runner.Execution;

sealed class RunMetrics
{
    private static readonly HashSet<string> ReadTools = ["get_scene", "get_entity"];
    private static readonly HashSet<string> WriteTools =
    [
        "add_entity",
        "move_entity",
        "rotate_entity",
        "scale_entity",
        "set_material",
        "rename_entity",
        "remove_entity",
        "submit_scene",
    ];

    public Dictionary<string, int> ToolCallsByTool { get; } = [];
    public List<ToolErrorLog> ToolErrors { get; } = [];
    public List<ToolWarningLog> ToolWarnings { get; } = [];
    public long LlmCalls { get; private set; }
    public long InputTokens { get; private set; }
    public long OutputTokens { get; private set; }
    public int SceneReads { get; private set; }
    public int SceneWrites { get; private set; }

    public void RecordResponse(ModelResponse response)
    {
        LlmCalls++;
        InputTokens += response.InputTokens;
        OutputTokens += response.OutputTokens;
    }

    public void RecordToolCall(string toolName)
    {
        ToolCallsByTool[toolName] = ToolCallsByTool.GetValueOrDefault(toolName) + 1;

        if (ReadTools.Contains(toolName))
            SceneReads++;
        else if (WriteTools.Contains(toolName))
            SceneWrites++;
    }

    public void RecordToolResult(
        string toolName,
        string? errorMessage,
        bool isError,
        IReadOnlyList<string> warnings
    )
    {
        var at = DateTimeOffset.UtcNow;
        if (isError)
            ToolErrors.Add(new ToolErrorLog(toolName, errorMessage ?? "tool call failed", at));

        ToolWarnings.AddRange(
            warnings.Select(warning => new ToolWarningLog(toolName, warning, at))
        );
    }
}
