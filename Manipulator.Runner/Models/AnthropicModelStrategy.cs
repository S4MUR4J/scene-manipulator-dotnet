using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using AnthropicRole = Anthropic.Models.Messages.Role;
using AnthropicTool = Anthropic.Models.Messages.Tool;

namespace Manipulator.Runner.Models;

sealed class AnthropicModelStrategy(AnthropicClient client, string model) : IModelStrategy
{
    private readonly List<MessageParam> _messages = [];
    private IReadOnlyList<ToolUnion>? _tools;

    public async Task<ModelResponse> StartAsync(
        string systemPrompt,
        string prompt,
        IReadOnlyList<ModelTool> tools,
        CancellationToken cancellationToken
    )
    {
        _messages.Add(new MessageParam { Role = AnthropicRole.User, Content = prompt });
        _tools = tools
            .Select(tool =>
                (ToolUnion)
                    new AnthropicTool
                    {
                        Name = tool.Name,
                        Description = tool.Description,
                        InputSchema = AgentLoop.ToInputSchema(tool.InputSchema),
                    }
            )
            .ToList();

        return await CompleteAsync(systemPrompt, cancellationToken);
    }

    public async Task<ModelResponse> ContinueAsync(
        IReadOnlyList<ModelToolResult> toolResults,
        CancellationToken cancellationToken
    )
    {
        _messages.Add(
            new MessageParam
            {
                Role = AnthropicRole.User,
                Content = toolResults
                    .Select(result =>
                        (ContentBlockParam)
                            new ToolResultBlockParam(result.ToolCallId)
                            {
                                Content = result.Content,
                                IsError = result.IsError,
                            }
                    )
                    .ToList(),
            }
        );

        return await CompleteAsync(RunnerConstants.SystemPrompt, cancellationToken);
    }

    private async Task<ModelResponse> CompleteAsync(
        string systemPrompt,
        CancellationToken cancellationToken
    )
    {
        var response = await client.Messages.Create(
            new MessageCreateParams
            {
                Model = model,
                MaxTokens = RunnerConstants.MaxTokens,
                System = systemPrompt,
                Messages = _messages,
                Tools = _tools!,
            },
            cancellationToken
        );

        _messages.Add(
            new MessageParam
            {
                Role = AnthropicRole.Assistant,
                Content = response
                    .Content.Select(block => new ContentBlockParam(block.Json))
                    .ToList(),
            }
        );

        return new ModelResponse(
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.StopReason?.ToString() ?? "end_turn",
            response
                .Content.Select(block => block.TryPickToolUse(out var toolUse) ? toolUse : null)
                .Where(toolUse => toolUse is not null)
                .Select(toolUse => new ModelToolCall(
                    toolUse!.ID,
                    toolUse.Name,
                    JsonSerializer.SerializeToElement(toolUse.Input)
                ))
                .ToList()
        );
    }
}
