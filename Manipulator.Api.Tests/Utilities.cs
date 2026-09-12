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
            new Scene
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Test Scene 1",
            },
            new Scene
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "Test Scene 2",
            },
            new Scene
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "Test Scene 3",
            },
        };
    }
}
