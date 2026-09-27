using System.Text.Json;

namespace Manipulator.Runner.Models;

public sealed record ModelTool(string Name, string? Description, JsonElement InputSchema);

public sealed record ModelToolCall(string Id, string Name, JsonElement Arguments);

public sealed record ModelToolResult(string ToolCallId, string Content, bool IsError);

public sealed record ModelResponse(
    long InputTokens,
    long OutputTokens,
    string StopReason,
    IReadOnlyList<ModelToolCall> ToolCalls
);

public interface IModelStrategy
{
    Task<ModelResponse> StartAsync(
        string systemPrompt,
        string prompt,
        IReadOnlyList<ModelTool> tools,
        CancellationToken cancellationToken
    );

    Task<ModelResponse> ContinueAsync(
        IReadOnlyList<ModelToolResult> toolResults,
        CancellationToken cancellationToken
    );
}
