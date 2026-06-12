namespace Manipulator.Core.Ecs;

public class Scene
{
    private readonly Dictionary<string, Entity> _entities = new Dictionary<string, Entity>();

    // Queries
    public IReadOnlyDictionary<string, Entity> Entities => _entities.AsReadOnly();
    public long Version { get; private set; }

    public Entity? GetEntity(string id)
    {
        _entities.TryGetValue(id, out var entity);
        return entity;
    }

    public bool HasEntity(string id)
    {
        return _entities.ContainsKey(id);
    }

    public int Count => _entities.Count;

    // Mutations
    internal Entity AddEntity(Entity entity)
    {
        if (!_entities.TryAdd(entity.Id, entity))
            throw new InvalidOperationException($"Entity '{entity.Id}' already exists.");

        Version++;
        return entity;
    }

    internal bool RemoveEntity(string id)
    {
        var removed = _entities.Remove(id);
        if (removed)
            Version++;
        return removed;
    }

    internal void Clear()
    {
        _entities.Clear();
        Version++;
    }
}
