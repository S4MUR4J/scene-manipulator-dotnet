using System.Text.Json.Nodes;
using Manipulator.Runner.Logging;
using Manipulator.Runner.Models;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Manipulator.Runner.Execution;

static class ToolCallExecutor
{
    public static async Task<List<ToolCallExecution>> ExecuteAsync(
        McpClient mcpClient,
        IReadOnlyList<ModelToolCall> toolCalls,
        RunMetrics metrics,
        CancellationToken cancellationToken
    )
    {
        var executions = new List<ToolCallExecution>();
        foreach (var toolCall in toolCalls)
        {
            metrics.RecordToolCall(toolCall.Name);
            var execution = await ExecuteAsync(mcpClient, toolCall, cancellationToken);
            metrics.RecordToolResult(toolCall.Name, execution.ErrorMessage, execution.IsError);
            executions.Add(execution);
        }

        return executions;
    }

    private static async Task<ToolCallExecution> ExecuteAsync(
        McpClient mcpClient,
        ModelToolCall toolCall,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await mcpClient.CallToolAsync(
                toolCall.Name,
                toolCall
                    .Arguments.EnumerateObject()
                    .ToDictionary(property => property.Name, property => (object?)property.Value),
                cancellationToken: cancellationToken
            );
            var resultText = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
            var envelope = resultText.Length > 0 ? JsonNode.Parse(resultText) : null;
            var errorMessage = envelope?["error"]?.GetValue<string>();
            return new ToolCallExecution(
                toolCall.Id,
                toolCall.Name,
                toolCall.Arguments.GetRawText(),
                resultText,
                result.IsError == true || errorMessage is not null,
                errorMessage
            );
        }
        catch (Exception ex)
        {
            return new ToolCallExecution(
                toolCall.Id,
                toolCall.Name,
                toolCall.Arguments.GetRawText(),
                $"Tool call failed: {ex.Message}",
                true,
                ex.Message
            );
        }
    }
}

sealed record ToolCallExecution(
    string Id,
    string Name,
    string ArgumentsJson,
    string ResultText,
    bool IsError,
    string? ErrorMessage
)
{
    public ToolCallLog ToLog() => new ToolCallLog(Name, ArgumentsJson, IsError, ErrorMessage);
}
