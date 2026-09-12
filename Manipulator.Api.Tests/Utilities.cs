using Manipulator.Api.Domain;
using Manipulator.Api.Infrastructure;

namespace Manipulator.Api.Tests;

public static class Utilities
{
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
        return new List<Scene>
        {
            new Scene { Id = Guid.Parse(TestSceneIds.SceneOne), Name = "Test Scene 1" },
            new Scene { Id = Guid.Parse(TestSceneIds.SceneTwo), Name = "Test Scene 2" },
            new Scene { Id = Guid.Parse(TestSceneIds.SceneThree), Name = "Test Scene 3" },
        };
    }
}
