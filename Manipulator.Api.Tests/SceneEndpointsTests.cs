using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Manipulator.Api.Domain;
using Manipulator.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Manipulator.Api.Tests;

// TODO: Create TestSceneBuilder class for more complex tests.
public class SceneEndpointsTests(ScenesApiFactory<Program> factory)
    : IClassFixture<ScenesApiFactory<Program>>,
        IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateAuthorizedClient();

    public Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Utilities.ReinitializeDbForTests(dbContext);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    # region GET /scenes

    [Fact]
    public async Task GetAllScenes()
    {
        // Act
        var response = await _client.GetAsync("/scenes/");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GET /scenes/{id}

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task GetSceneById(Guid id)
    {
        // Act
        var response = await _client.GetAsync($"/scenes/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task GetNonExistingSceneById(Guid id)
    {
        // Act
        var response = await _client.GetAsync($"/scenes/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /scenes

    [Fact]
    public async Task CreateScene()
    {
        // Arrange
        var scene = new Scene { Name = "New Scene" };

        // Act
        var response = await _client.PostAsJsonAsync("/scenes/", scene);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region PUT /scenes

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task UpdateScene(Guid id)
    {
        // Arrange
        var scene = new Scene { Id = id, Name = "Updated Scene" };

        // Act
        var response = await _client.PutAsJsonAsync($"/scenes/{id}", scene);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task UpdateNonExistingScene(Guid id)
    {
        // Arrange
        var scene = new Scene { Id = id, Name = "Updated Scene" };

        // Act
        var response = await _client.PutAsJsonAsync($"/scenes/{id}", scene);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /scenes

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task DeleteScene(Guid id)
    {
        // Act
        var response = await _client.DeleteAsync($"/scenes/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task DeleteNonExistingScene(Guid id)
    {
        // Act
        var response = await _client.DeleteAsync($"/scenes/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
