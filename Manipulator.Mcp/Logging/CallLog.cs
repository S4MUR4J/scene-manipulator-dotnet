using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Manipulator.Mcp.Logging;

/// <summary>
/// Append-only record of everything that happened in a session: every tool call with its arguments
/// and result, every scene event published on the <c>EventBus</c>, and every error. Reads and
/// failures publish no events, so the call itself is logged too — the event stream alone would miss
/// exactly the operations the experiment is measuring.
/// </summary>
public sealed class CallLog(string sessionId, string? filePath = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Lock _gate = new Lock();
    private readonly List<CallLogEntry> _entries = [];
    private long _seq;

    public string SessionId => sessionId;

    public IReadOnlyList<CallLogEntry> Entries
    {
        get
        {
            lock (_gate)
                return _entries.ToList();
        }
    }

    public CallLogEntry Append(CallLogEntry entry)
    {
        lock (_gate)
        {
            var stamped = entry with
            {
                Seq = ++_seq,
                Timestamp = entry.Timestamp == default ? DateTimeOffset.UtcNow : entry.Timestamp,
                SessionId = sessionId,
            };
            _entries.Add(stamped);
            WriteLine(stamped);
            return stamped;
        }
    }

    public JsonArray ToJson()
    {
        var array = new JsonArray();
        foreach (var entry in Entries)
            array.Add(JsonSerializer.SerializeToNode(entry, JsonOptions));
        return array;
    }

    private void WriteLine(CallLogEntry entry)
    {
        if (filePath is null)
            return;

        var line = JsonSerializer.Serialize(entry, JsonOptions);
        File.AppendAllText(filePath, line + Environment.NewLine);
    }
}
