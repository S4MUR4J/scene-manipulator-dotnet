using Manipulator.Core.Ecs;
using Manipulator.Core.Ecs.Components;
using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Scoring;

public static class ComponentValueEvaluator
{
    public static RequirementResult Evaluate(
        ComponentValueRequirement requirement,
        ScenarioSpec spec,
        Scene scene
    )
    {
        var entities = RoleResolver.Resolve(scene, spec.Roles, requirement.Role);
        if (entities.Count == 0)
            return new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: $"role '{requirement.Role}' has no matching entities."
            );

        var failures = entities
            .Select(entity => CheckEntity(entity, requirement))
            .Where(reason => reason is not null)
            .ToList();

        return failures.Count == 0
            ? new RequirementResult(
                requirement.Id,
                Passed: true,
                Reason: "All matching entities satisfy the constraint."
            )
            : new RequirementResult(
                requirement.Id,
                Passed: false,
                Reason: string.Join("; ", failures)
            );
    }

    private static string? CheckEntity(Entity entity, ComponentValueRequirement requirement)
    {
        var component = entity.Get(requirement.Component);
        if (component is null)
            return $"entity '{entity.Id}' has no component '{requirement.Component}'";

        var field = requirement.Field.ToLowerInvariant();
        var constraint = requirement.Constraint;

        if (constraint.ExpectedValue is not null)
            return CheckExpectedValue(entity, component, field, constraint.ExpectedValue);

        if (constraint.HueMinDeg is not null || constraint.HueMaxDeg is not null)
            return CheckHueRange(
                entity,
                component,
                field,
                constraint.HueMinDeg,
                constraint.HueMaxDeg
            );

        return CheckNumericRange(entity, component, field, constraint.Min, constraint.Max);
    }

    private static string? CheckExpectedValue(
        Entity entity,
        IComponent component,
        string field,
        string expected
    )
    {
        var actual = GetStringField(component, field);
        if (actual is null)
            return $"entity '{entity.Id}': field '{field}' is not a recognized string field on {component.Type}";

        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"entity '{entity.Id}': {component.Type}.{field} expected '{expected}', got '{actual}'";
    }

    private static string? CheckHueRange(
        Entity entity,
        IComponent component,
        string field,
        double? minDeg,
        double? maxDeg
    )
    {
        if (component is not MeshRenderer meshRenderer || field != "color")
            return $"entity '{entity.Id}': field '{field}' on {component.Type} has no hue";

        var hue = ColorHue.FromHex(meshRenderer.Color);
        var min = minDeg ?? 0.0;
        var max = maxDeg ?? 360.0;
        var inRange = min <= max ? hue >= min && hue <= max : hue >= min || hue <= max;

        return inRange
            ? null
            : $"entity '{entity.Id}': {component.Type}.color hue {hue:F1}deg outside [{min}, {max}]deg";
    }

    private static string? CheckNumericRange(
        Entity entity,
        IComponent component,
        string field,
        double? min,
        double? max
    )
    {
        var value = GetNumericField(component, field);
        if (value is null)
            return $"entity '{entity.Id}': field '{field}' is not a recognized numeric field on {component.Type}";

        if (min is not null && value < min)
            return $"entity '{entity.Id}': {component.Type}.{field} {value} is below minimum {min}";
        if (max is not null && value > max)
            return $"entity '{entity.Id}': {component.Type}.{field} {value} is above maximum {max}";
        return null;
    }

    private static string? GetStringField(IComponent component, string field) =>
        (component, field) switch
        {
            (MeshRenderer meshRenderer, "color") => meshRenderer.Color,
            _ => null,
        };

    private static double? GetNumericField(IComponent component, string field) =>
        (component, field) switch
        {
            (Transform transform, "scale.x") => transform.Scale.X,
            (Transform transform, "scale.y") => transform.Scale.Y,
            (Transform transform, "scale.z") => transform.Scale.Z,
            (MeshRenderer meshRenderer, "opacity") => meshRenderer.Opacity,
            (MeshRenderer meshRenderer, "metalness") => meshRenderer.Metalness,
            (MeshRenderer meshRenderer, "roughness") => meshRenderer.Roughness,
            _ => null,
        };
}
