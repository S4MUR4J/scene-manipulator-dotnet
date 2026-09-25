namespace Manipulator.Core.Commands;

public record SetMaterialCommand(
    string EntityId,
    string? Color = null,
    float? Opacity = null,
    float? Metalness = null,
    float? Roughness = null,
    long? ExpectedVersion = null
) : IEntityTargetCommand
{
    public string Type => "SetMaterial";
}
