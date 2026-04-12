using Manipulator.Core.Ecs;

namespace Manipulator.Core.Tests.Helpers;

public class EntityBuilder(string id)
{
    private readonly Entity _entity = new Entity(id);

    public EntityBuilder WithComponent<T>(T component) where T : class, IComponent
    {
        _entity.Set(component);
        return this;
    }

    public Entity Build() => _entity;
}
