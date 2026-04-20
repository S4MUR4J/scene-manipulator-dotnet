using Manipulator.Core.Ecs;

namespace Manipulator.Core.Tests.Helpers;

public class EntityBuilder(string id)
{
    private readonly Entity _entity = new Entity(id);
    private bool _built;

    public EntityBuilder WithComponent<T>(T component)
        where T : class, IComponent
    {
        if (_built)
            throw new InvalidOperationException(
                "Cannot modify EntityBuilder after Build() has been called."
            );
        _entity.Set(component);
        return this;
    }

    public Entity Build()
    {
        _built = true;
        return _entity;
    }
}
