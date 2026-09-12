using FluentValidation;
using Manipulator.Api;
using Manipulator.Api.Domain;
using Manipulator.Api.Infrastructure;
using Manipulator.Api.Presentation.Scenes;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseSnakeCaseNamingConvention()
);
builder.Services.AddScoped<IValidator<Scene>, SceneValidator>();

var app = builder.Build();

app.UseMiddleware<ApiKeyAuthMiddleware>();

app.RegisterScenesEndpoints();

app.Run();
