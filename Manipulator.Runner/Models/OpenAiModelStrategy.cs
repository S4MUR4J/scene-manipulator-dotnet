using System.Text.Json;
using OpenAI.Chat;
using Manipulator.Runner.Execution;

namespace Manipulator.Runner.Models;

sealed class OpenAiModelStrategy(ChatClient client) : IModelStrategy
{
    private readonly List<ChatMessage> _messages = [];
    private ChatCompletionOptions? _options;

    public async Task<ModelResponse> StartAsync(
        string systemPrompt,
        string prompt,
        IReadOnlyList<ModelTool> tools,
        CancellationToken cancellationToken
    )
    {
        _messages.Add(new SystemChatMessage(systemPrompt));
        _messages.Add(new UserChatMessage(prompt));
        _options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = (int)RunnerConstants.MaxTokens,
        };
        foreach (var tool in tools)
            _options.Tools.Add(
                ChatTool.CreateFunctionTool(
                    tool.Name,
                    tool.Description,
                    BinaryData.FromString(tool.InputSchema.GetRawText())
                )
            );

        return await CompleteAsync(cancellationToken);
    }

    public async Task<ModelResponse> ContinueAsync(
        IReadOnlyList<ModelToolResult> toolResults,
        CancellationToken cancellationToken
    )
    {
        _messages.AddRange(
            toolResults.Select(result => new ToolChatMessage(result.ToolCallId, result.Content))
        );
        return await CompleteAsync(cancellationToken);
    }

    private async Task<ModelResponse> CompleteAsync(CancellationToken cancellationToken)
    {
        var completion = (
            await client.CompleteChatAsync(_messages, _options!, cancellationToken)
        ).Value;
        _messages.Add(new AssistantChatMessage(completion));

        return new ModelResponse(
            completion.Usage.InputTokenCount,
            completion.Usage.OutputTokenCount,
            completion.FinishReason.ToString(),
            completion
                .ToolCalls.Select(toolCall => new ModelToolCall(
                    toolCall.Id,
                    toolCall.FunctionName,
                    JsonDocument.Parse(toolCall.FunctionArguments.ToString()).RootElement.Clone()
                ))
                .ToList()
        );
    }
}
