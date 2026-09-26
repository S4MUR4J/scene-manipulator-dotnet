using System.ComponentModel;
using Manipulator.Core.Commands;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

[McpServerToolType]
public sealed class SceneWriteTools(CommandDispatcher dispatcher)
{
    [McpServerTool(Name = "add_entity", Destructive = false, Idempotent = false)]
    [Description(ToolDescriptions.AddEntity)]
    public string AddEntity()
    {
        throw new NotImplementedException();
    }
}
