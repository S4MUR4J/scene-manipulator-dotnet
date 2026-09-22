using System.Linq.Expressions;
using Manipulator.Api.Domain;

namespace Manipulator.Api.Presentation.Scenes.Contracts;

public record SceneRes(string Name, DateTime Created, DateTime Updated)
{
    public static readonly Expression<Func<Scene, SceneRes>> FromDomain = scene => new SceneRes(
        scene.Name ?? string.Empty,
        scene.Created,
        scene.Updated
    );
}
