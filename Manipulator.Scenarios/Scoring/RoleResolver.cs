using Manipulator.Core.Ecs;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

/// <summary>
/// Resolves a role name to every entity in the scene matching its <see cref="RoleSelector"/>
/// (the "all" quantifier - see <see cref="RoleSelector"/> for why a single match isn't picked).
/// Assumes <paramref name="roleName"/> is declared in <paramref name="roles"/>, which
/// <c>ScenarioSpecDtoValidator</c> already guarantees for every role a requirement references.
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

    /// <summary>Resolves and flattens several roles, e.g. a requirement's Subjects or References.</summary>
    public static IReadOnlyList<Entity> ResolveAll(
        Scene scene,
        IReadOnlyDictionary<string, RoleSelector> roles,
        IReadOnlyList<string> roleNames
    ) => roleNames.SelectMany(roleName => Resolve(scene, roles, roleName)).ToList();
}
