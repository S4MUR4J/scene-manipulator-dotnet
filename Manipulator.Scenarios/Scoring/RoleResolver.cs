using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

/// <summary>
/// Returns every match, not just one - see <see cref="RoleSelector"/> for why. Assumes the role is
/// declared in <c>roles</c>, which <c>ScenarioSpecDtoValidator</c> already guarantees.
/// </summary>
public static class RoleResolver
{
    public static IReadOnlyList<Entity> Resolve(
        Scene scene,
        IReadOnlyDictionary<string, RoleSelector> roles,
        string roleName
    )
    {
        var selector = roles[roleName];
        return scene.Entities.Values.Where(selector.Matches).ToList();
    }

    public static IReadOnlyList<Entity> ResolveAll(
        Scene scene,
        IReadOnlyDictionary<string, RoleSelector> roles,
        IReadOnlyList<string> roleNames
    ) => roleNames.SelectMany(roleName => Resolve(scene, roles, roleName)).ToList();
}
