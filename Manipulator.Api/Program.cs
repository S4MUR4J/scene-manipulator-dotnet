using Asp.Versioning;
using FluentValidation;
using Manipulator.Api.Domain;
using Manipulator.Api.Infrastructure;
using Manipulator.Api.Presentation.Middleware;
using Manipulator.Api.Presentation.Scenes;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["Authentication:ApiKey"]))
    throw new InvalidOperationException(
        "Missing required configuration value 'Authentication:ApiKey'."
    );

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseSnakeCaseNamingConvention()
);
builder.Services.AddScoped<IValidator<Scene>, SceneValidator>();

builder.Services.AddOpenApi();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ApiKeyAuthMiddleware>();

var apiVersionSet = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1)).Build();
var versionGroup = app.MapGroup("api/v{apiVersion:apiVersion}").WithApiVersionSet(apiVersionSet);

versionGroup.RegisterScenesEndpoints();

app.Run();
