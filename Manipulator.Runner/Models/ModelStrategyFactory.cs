using Anthropic;
using OpenAI.Chat;

namespace Manipulator.Runner.Models;

internal sealed class ModelStrategyFactory(string anthropicApiKey, string openAiApiKey)
{
    public IModelStrategy Create(RunConfig config) =>
        config.Provider switch
        {
            ModelProvider.Anthropic => new AnthropicModelStrategy(
                new AnthropicClient { ApiKey = anthropicApiKey },
                config.Model
            ),
            ModelProvider.OpenAi => new OpenAiModelStrategy(new ChatClient(config.Model, openAiApiKey)),
            _ => throw new ArgumentOutOfRangeException(nameof(config), config.Provider, null),
        };
}
