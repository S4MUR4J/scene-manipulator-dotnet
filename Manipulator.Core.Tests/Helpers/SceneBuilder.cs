using Manipulator.Core.Ecs;

namespace Manipulator.Core.Tests.Helpers;

public class SceneBuilder
{
    private readonly Scene _scene = new Scene();
    private int _entityCounter;

    private const string DefaultEntityTag = "entity";

    public static string Id(int index, string tag = DefaultEntityTag) => TestUtils.Tag(tag, index);

    public SceneBuilder WithEntity(Action<EntityBuilder>? configure = null)
        => WithEntity(Id(++_entityCounter), configure);

    public SceneBuilder WithEntity(string id, Action<EntityBuilder>? configure = null)
    {
        var builder = new EntityBuilder(id);
        configure?.Invoke(builder);
        _scene.AddEntity(builder.Build());
        return this;
    }

    public SceneBuilder WithEntities(int count, string tag = DefaultEntityTag)
    {
        for (var i = 1; i <= count; i++)
            _scene.AddEntity(new Entity(Id(i, tag)));
        return this;
    }

    public Scene Build() => _scene;
}
