namespace Manipulator.Core.Ecs;

public class Entity
{
    public string Id { get; }
    private readonly Dictionary<string, IComponent> _components =
        new Dictionary<string, IComponent>();

    public Entity(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var trimmed = id.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Entity id cannot be empty or whitespace.", nameof(id));

        Id = trimmed;
    }

    // Component access
    public T? Get<T>()
        where T : class, IComponent
    {
        var typeName = typeof(T).Name;
        return Get(typeName) as T;
    }

    public IComponent? Get(string componentType)
    {
        _components.TryGetValue(componentType, out var component);
        return component;
    }

    public bool Has<T>()
        where T : class, IComponent
    {
        var typeName = typeof(T).Name;
        return Has(typeName);
    }

    public bool Has(string componentType)
    {
        return _components.ContainsKey(componentType);
    }

    public IReadOnlyDictionary<string, IComponent> Components => _components.AsReadOnly();

    // Mutations
    internal void Set<T>(T component)
        where T : class, IComponent
    {
        var typeName = typeof(T).Name;
        Set(typeName, component);
    }

    internal void Set(string componentType, IComponent component)
    {
        _components[componentType] = component;
    }

    internal bool Remove<T>()
        where T : class, IComponent
    {
        var typeName = typeof(T).Name;
        return Remove(typeName);
    }

    internal bool Remove(string componentType)
    {
        return _components.Remove(componentType);
    }
}
