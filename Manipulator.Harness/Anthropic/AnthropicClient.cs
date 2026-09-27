using System.Text;
using System.Text.Json.Nodes;

namespace Manipulator.Harness.Anthropic;

public sealed record AnthropicResponse(
    JsonArray Content,
    string StopReason,
    int InputTokens,
    int OutputTokens
);

public sealed class AnthropicApiException(string message) : Exception(message);

/// <summary>
/// Thin wrapper over the Anthropic Messages API (no SDK dependency). Model parameters
/// (temperature, max tokens, API version) come from <see cref="HarnessConstants"/> so every
/// approach calls the API the same way.
/// </summary>
public sealed class AnthropicClient(HttpClient httpClient, string apiKey)
{
    public async Task<AnthropicResponse> SendAsync(
        string model,
        string system,
        JsonArray messages,
        JsonArray tools,
        CancellationToken cancellationToken
    )
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = HarnessConstants.MaxTokens,
            ["temperature"] = HarnessConstants.Temperature,
            ["system"] = system,
            ["messages"] = messages.DeepClone(),
            ["tools"] = tools.DeepClone(),
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            HarnessConstants.AnthropicEndpoint
        )
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", HarnessConstants.AnthropicVersion);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new AnthropicApiException(
                $"Anthropic API returned {(int)response.StatusCode}: {responseText}"
            );

        var root =
            JsonNode.Parse(responseText) as JsonObject
            ?? throw new AnthropicApiException("Anthropic API returned an unparseable response.");

        var content = (root["content"] as JsonArray)?.DeepClone() as JsonArray ?? [];
        var stopReason = root["stop_reason"]?.GetValue<string>() ?? "end_turn";
        var usage = root["usage"] as JsonObject;
        var inputTokens = usage?["input_tokens"]?.GetValue<int>() ?? 0;
        var outputTokens = usage?["output_tokens"]?.GetValue<int>() ?? 0;

        return new AnthropicResponse(content, stopReason, inputTokens, outputTokens);
    }
}
