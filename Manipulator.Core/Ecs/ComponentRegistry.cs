namespace Manipulator.Core.Ecs;

public class ComponentRegistry
{
    private readonly Dictionary<string, Type> _components = new Dictionary<string, Type>();

    // TODO: Register(string, Type) — name validation gap.
    // Currently accepts any string as key, including empty or mismatched names.
    // Three options under consideration:
    //   1. Enforce type == componentType.Name — consistent with Register<T>(), but makes the string param redundant.
    //   2. Replace with Register(Type) — cleaner API for runtime registration; key derived from componentType.Name internally.
    //   3. Keep aliasing, add identifier-format validation — valid if aliasing is a real use case.
    // Decision deferred until there is a concrete consumer of this overload.
    //
    // Register<T>() uses typeof(T).Name as the key, assuming it matches IComponent.Type.
    // This holds by convention (all components use nameof(ClassName)) but is not enforced.
    public void Register<T>()
        where T : class, IComponent
    {
        _components[typeof(T).Name] = typeof(T);
    }

    public void Register(string type, Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException(
                $"Type {componentType.Name} must implement IComponent",
                nameof(componentType)
            );

        _components[type] = componentType;
    }

    public Type? Resolve(string type)
    {
        _components.TryGetValue(type, out var componentType);
        return componentType;
    }

    public bool IsRegistered(string type)
    {
        return _components.ContainsKey(type);
    }

    public IReadOnlyCollection<string> RegisteredTypes => _components.Keys.ToList().AsReadOnly();
}
