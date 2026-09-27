using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Contracts;

record RoleSelectorDto(List<string>? NameContains, List<string>? Geometry, int? Count)
{
    public RoleSelector ToDomain()
    {
        var geometry = Geometry
            ?.Select(g => Enum.Parse<GeometryType>(g, ignoreCase: true))
            .ToList();

        return new RoleSelector(NameContains!, geometry, Count!.Value);
    }
}
