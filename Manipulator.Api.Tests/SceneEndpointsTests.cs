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
    public async Task GetAllScenes_ScenesExist_ReturnsOkWithScenes()
    {
        // Act
        var response = await _client.GetAsync($"{TestRoutes.Scenes}/");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GET /scenes/{id}

    [Theory]
    [InlineData(TestSceneIds.SceneOne)]
    [InlineData(TestSceneIds.SceneTwo)]
    [InlineData(TestSceneIds.SceneThree)]
    public async Task GetSceneById_SceneExists_ReturnsOkWithScene(Guid id)
    {
        // Act
        var response = await _client.GetAsync($"{TestRoutes.Scenes}/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(TestSceneIds.NonExistent)]
    public async Task GetSceneById_SceneDoesNotExist_ReturnsNotFound(Guid id)
    {
        // Act
        var response = await _client.GetAsync($"{TestRoutes.Scenes}/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /scenes

    [Fact]
    public async Task CreateScene_ValidScene_ReturnsCreated()
    {
        // Arrange
        var scene = new Scene { Name = "New Scene" };

        // Act
        var response = await _client.PostAsJsonAsync($"{TestRoutes.Scenes}/", scene);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(TestSceneIds.SceneOne, null)]
    [InlineData(TestSceneIds.SceneTwo, "")]
    [InlineData(TestSceneIds.SceneThree, "   ")]
    [InlineData(TestSceneIds.SceneOne, "TooLongNameThatCannotBeHandled")]
    public async Task CreateScene_InvalidName_ReturnsBadRequest(Guid id, string? name)
    {
        // Arrange
        var scene = new Scene { Id = id, Name = name };

        // Act
        var response = await _client.PostAsJsonAsync($"{TestRoutes.Scenes}/", scene);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /scenes

    [Theory]
    [InlineData(TestSceneIds.SceneOne)]
    [InlineData(TestSceneIds.SceneTwo)]
    [InlineData(TestSceneIds.SceneThree)]
    public async Task UpdateScene_SceneExists_ReturnsNoContent(Guid id)
    {
        // Arrange
        var scene = new Scene { Id = id, Name = "Updated Scene" };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestRoutes.Scenes}/{id}", scene);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(TestSceneIds.SceneOne, null)]
    [InlineData(TestSceneIds.SceneTwo, "")]
    [InlineData(TestSceneIds.SceneThree, "   ")]
    [InlineData(TestSceneIds.SceneOne, "TooLongNameThatCannotBeHandled")]
    public async Task UpdateScene_InvalidName_ReturnsBadRequest(Guid id, string? name)
    {
        // Arrange
        var scene = new Scene { Id = id, Name = name };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestRoutes.Scenes}/{id}", scene);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(TestSceneIds.NonExistent)]
    public async Task UpdateScene_SceneDoesNotExist_ReturnsNotFound(Guid id)
    {
        // Arrange
        var scene = new Scene { Id = id, Name = "Updated Scene" };

        // Act
        var response = await _client.PutAsJsonAsync($"{TestRoutes.Scenes}/{id}", scene);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /scenes

    [Theory]
    [InlineData(TestSceneIds.SceneOne)]
    [InlineData(TestSceneIds.SceneTwo)]
    [InlineData(TestSceneIds.SceneThree)]
    public async Task DeleteScene_SceneExists_ReturnsNoContent(Guid id)
    {
        // Act
        var response = await _client.DeleteAsync($"{TestRoutes.Scenes}/{id}");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(TestSceneIds.NonExistent)]
    public async Task DeleteScene_SceneDoesNotExist_ReturnsNotFound(Guid id)
    {
        // Act
        var response = await _client.DeleteAsync($"{TestRoutes.Scenes}/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
