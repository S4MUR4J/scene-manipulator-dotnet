using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

[McpServerToolType]
public sealed class SceneWriteTools()
{
    [McpServerTool(Name = "add_entity", Destructive = false, Idempotent = false)]
    [Description("Missing description")]
    public string AddEntity()
    {
        throw new NotImplementedException();
    }
}
