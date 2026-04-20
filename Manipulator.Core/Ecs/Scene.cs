namespace Manipulator.Core.Ecs;

public class Scene
{
    private readonly Dictionary<string, Entity> _entities = new Dictionary<string, Entity>();

    // Queries
    public IReadOnlyDictionary<string, Entity> Entities => _entities.AsReadOnly();

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
        _entities[entity.Id] = entity;
        return entity;
    }

    internal bool RemoveEntity(string id)
    {
        return _entities.Remove(id);
    }

    internal void Clear()
    {
        _entities.Clear();
    }
}
