namespace Manipulator.Core.Ecs.Components;

public record EntityName(string Value = "") : IComponent
{
    public string Type => nameof(EntityName);
}
