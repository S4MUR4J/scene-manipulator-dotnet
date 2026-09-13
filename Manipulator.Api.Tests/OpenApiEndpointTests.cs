using System.Net;
using FluentAssertions;

namespace Manipulator.Api.Tests;

public class OpenApiEndpointTests(ScenesApiFactory<Program> factory)
    : IClassFixture<ScenesApiFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateAuthorizedClient();

    [Fact]
    public async Task GetOpenApiDocument_ReturnsOkWithJson()
    {
        // Act
        var response = await _client.GetAsync(TestRoutes.OpenApi);

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }
}
