using Anthropic;
using OpenAI.Chat;

namespace Manipulator.Runner.Models;

sealed class ModelStrategyFactory(IConfiguration configuration)
{
    public IModelStrategy Create(RunConfig config) =>
        config.Provider switch
        {
            ModelProvider.Anthropic => new AnthropicModelStrategy(
                new AnthropicClient { ApiKey = GetRequiredApiKey("Anthropic:ApiKey") },
                config.Model
            ),
            ModelProvider.OpenAi => new OpenAiModelStrategy(
                new ChatClient(config.Model, GetRequiredApiKey("OpenAI:ApiKey"))
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(config), config.Provider, null),
        };

    private string GetRequiredApiKey(string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing required configuration value '{key}'.");
}
