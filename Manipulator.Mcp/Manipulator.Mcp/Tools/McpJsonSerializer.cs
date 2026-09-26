using System.Text.Encodings.Web;
using System.Text.Json;
using Manipulator.Core.Results;

namespace Manipulator.Mcp.Tools;

public static class McpJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serialize<T>(ManipulatorResult<T> result)
    {
        return JsonSerializer.Serialize(result, Options);
    }
}
