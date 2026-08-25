using Manipulator.Api;
using Manipulator.Api.Infrastructure;
using Manipulator.Api.Presentation.Scenes;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseSnakeCaseNamingConvention()
);

var app = builder.Build();

app.UseMiddleware<ApiKeyAuthMiddleware>();

app.RegisterScenesEndpoints();

app.Run();
