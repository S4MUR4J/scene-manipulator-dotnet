using System.ComponentModel;
using Manipulator.Core.Ecs;
using Manipulator.Core.Serialization;
using ModelContextProtocol.Server;

namespace Manipulator.Mcp.Tools;

[McpServerToolType]
public sealed class SceneReadTools(Scene scene)
{
    [McpServerTool(Name = "get_scene", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetScene)]
    public string GetScene()
    {
        return SceneSerializer.Serialize(scene);
    }

    [McpServerTool(Name = "get_entity", ReadOnly = true, Idempotent = true)]
    [Description(ToolDescriptions.GetEntity)]
    public string GetEntity()
    {
        throw new NotImplementedException();
    }
}
