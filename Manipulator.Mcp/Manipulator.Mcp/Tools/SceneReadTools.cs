using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

[McpServerToolType]
public sealed class SceneReadTools()
{
    [McpServerTool(Name = "get_scene", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetScene)]
    public string GetScene()
    {
        throw new NotImplementedException();
        Console.WriteLine("GetScene");
        return string.Empty;
    }

    [McpServerTool(Name = "get_entity", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetEntity)]
    public string GetEntity()
    {
        throw new NotImplementedException();
        Console.WriteLine("GetEntity");
        return string.Empty;
    }
}
