using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Manipulator.Api.Infrastructure;
using Manipulator.Api.Presentation.Scenes.Content.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Manipulator.Api.Tests;

public class ContentEndpointTests(ScenesApiFactory<Program> factory)
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

    #region GET /scenes/{id}/content

    [Theory]
    [InlineData(TestSceneIds.SceneOne)]
    [InlineData(TestSceneIds.SceneTwo)]
    [InlineData(TestSceneIds.SceneThree)]
    public async Task GetContent_ReturnsContent(Guid id)
    {
        // Act
        var response = await _client.GetAsync($"{TestRoutes.Scenes}/{id}/content");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<string>();
        content.Should().Be(Utilities.ExampleContent);
    }

    [Fact]
    public async Task GetContent_SceneDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"{TestRoutes.Scenes}/{nonExistentId}/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /scenes/{id}/content

    [Fact]
    public async Task UpdateContent_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.Parse(TestSceneIds.SceneOne);
        const string newContent = "Updated content";
        var requestBody = new SceneContentReq(newContent);

        // Act
        var response = await _client.PutAsJsonAsync(
            $"{TestRoutes.Scenes}/{id}/content",
            requestBody
        );

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scene = await dbContext.Scenes.FindAsync(id);
        scene.Should().NotBeNull();
        scene.Content.Should().Be(newContent);
    }

    [Fact]
    public async Task UpdateContent_SceneDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        const string newContent = "Updated content";
        var requestBody = new SceneContentReq(newContent);

        // Act
        var response = await _client.PutAsJsonAsync(
            $"{TestRoutes.Scenes}/{nonExistentId}/content",
            requestBody
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
