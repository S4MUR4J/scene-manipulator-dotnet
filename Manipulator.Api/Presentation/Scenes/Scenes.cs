using Manipulator.Api.Domain;
using Manipulator.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Manipulator.Api.Presentation.Scenes;

public static class Scenes
{
    public static void RegisterScenesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var scenesGroup = endpoints.MapGroup("scenes");

        scenesGroup.MapGet(
            "/",
            async (AppDbContext appDbContext) =>
            {
                var scenes = await appDbContext.Scenes.ToListAsync();
                return Results.Ok(scenes);
            }
        );

        scenesGroup.MapGet(
            "/{id:guid}",
            async (Guid id, AppDbContext appDbContext) =>
            {
                var scene = await appDbContext.Scenes.FindAsync(id);
                return scene is null ? Results.NotFound() : Results.Ok(scene);
            }
        );

        scenesGroup.MapPost(
            "/",
            async (Scene scene, AppDbContext appDbContext) =>
            {
                appDbContext.Scenes.Add(scene);
                await appDbContext.SaveChangesAsync();
                return Results.Created($"/scenes/{scene.Id}", scene);
            }
        );

        scenesGroup.MapPut(
            "/{id:guid}",
            async (Guid id, Scene scene, AppDbContext appDbContext) =>
            {
                var sceneFound = await appDbContext.Scenes.FindAsync(id);
                if (sceneFound is null)
                    return Results.NotFound();

                sceneFound.Name = scene.Name;
                sceneFound.Updated = DateTime.UtcNow;

                await appDbContext.SaveChangesAsync();
                return Results.NoContent();
            }
        );

        scenesGroup.MapDelete(
            "/{id:guid}",
            async (AppDbContext appDbContext, Guid id) =>
            {
                var scene = await appDbContext.Scenes.FindAsync(id);
                if (scene is null)
                    return Results.NotFound();

                appDbContext.Scenes.Remove(scene);
                await appDbContext.SaveChangesAsync();

                return Results.NoContent();
            }
        );
    }
}
