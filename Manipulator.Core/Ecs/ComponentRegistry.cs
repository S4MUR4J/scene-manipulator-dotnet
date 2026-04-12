namespace Manipulator.Core.Ecs;

public class ComponentRegistry
{
    private readonly Dictionary<string, Type> _components = new Dictionary<string, Type>();

    // TODO: Uses typeof(T).Name as the key, assuming it matches IComponent.Type.
    // This holds by convention (all components use nameof(ClassName)) but is not enforced.
    // A component with a custom Type value (e.g. an alias) registered via this overload
    // would be stored under the wrong key. Consider a static abstract IComponent.ComponentType
    // to enforce the contract at compile time if aliasing becomes a requirement.
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
