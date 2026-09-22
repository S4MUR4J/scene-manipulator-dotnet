using Manipulator.Api.Domain;
using Manipulator.Api.Infrastructure;

namespace Manipulator.Api.Tests;

public static class Utilities
{
    public const string ExampleContent = "Example content";

    public static void InitializeDbForTests(AppDbContext dbContext)
    {
        dbContext.Scenes.AddRange(GetSeedingScenes());
        dbContext.SaveChanges();
    }

    public static void ReinitializeDbForTests(AppDbContext dbContext)
    {
        dbContext.Scenes.RemoveRange(dbContext.Scenes);
        dbContext.SaveChanges();
        InitializeDbForTests(dbContext);
    }

    private static IReadOnlyList<Scene> GetSeedingScenes()
    {
        const string sceneName = "Test Scene";

        return new List<Scene>
        {
            new Scene
            {
                Id = Guid.Parse(TestSceneIds.SceneOne),
                Name = $"{sceneName} 1",
                Content = ExampleContent,
            },
            new Scene
            {
                Id = Guid.Parse(TestSceneIds.SceneTwo),
                Name = $"{sceneName} 2",
                Content = ExampleContent,
            },
            new Scene
            {
                Id = Guid.Parse(TestSceneIds.SceneThree),
                Name = $"{sceneName} 3",
                Content = ExampleContent,
            },
        };
    }
}
