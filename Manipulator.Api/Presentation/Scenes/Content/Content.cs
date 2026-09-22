using Manipulator.Api.Infrastructure;
using Manipulator.Api.Presentation.Scenes.Content.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manipulator.Api.Presentation.Scenes.Content;

public static class Content
{
    public static void RegisterContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var contentGroup = endpoints.MapGroup("{id:guid}/content/");

        contentGroup.MapGet(
            "/",
            async (AppDbContext appDbContext, Guid id) =>
            {
                var scene = await appDbContext.Scenes.FirstOrDefaultAsync(scene => scene.Id == id);
                if (scene == null)
                    return Results.NotFound();

                var content = scene.Content;
                return Results.Ok(content);
            }
        );

        contentGroup.MapPut(
            "/",
            async (AppDbContext appDbContext, Guid id, [FromBody] SceneContentReq req) =>
            {
                var scene = await appDbContext.Scenes.FirstOrDefaultAsync(scene => scene.Id == id);
                if (scene == null)
                    return Results.NotFound();

                scene.Content = req.Content;
                scene.Updated = DateTime.UtcNow;
                await appDbContext.SaveChangesAsync();

                return Results.NoContent();
            }
        );
    }
}
