namespace Manipulator.Core.Ecs;

public class ComponentRegistry
{
    private readonly Dictionary<string, Type> _components = new Dictionary<string, Type>();

    public void Register<T>()
        where T : class, IComponent, new()
    {
        var instance = new T();
        _components[instance.Type] = typeof(T);
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
