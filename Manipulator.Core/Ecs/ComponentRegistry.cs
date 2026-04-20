namespace Manipulator.Core.Ecs;

public class ComponentRegistry
{
    private readonly Dictionary<string, Type> _components = new Dictionary<string, Type>();

    public void Register<T>()
        where T : class, IComponent
    {
        _components[typeof(T).Name] = typeof(T);
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

    public IReadOnlyCollection<string> RegisteredTypes => _components.Keys;
}
