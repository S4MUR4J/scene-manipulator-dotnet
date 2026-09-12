using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Manipulator.Api.Domain;

namespace Manipulator.Api.Tests;

public class AuthorizationTests(ScenesApiFactory<Program> factory)
    : IClassFixture<ScenesApiFactory<Program>>
{
    private static readonly Guid SeededSceneId = Guid.Parse(TestSceneIds.SceneOne);

    public static IEnumerable<object[]> ProtectedRequests()
    {
        yield return [HttpMethod.Get, "/scenes/"];
        yield return [HttpMethod.Get, $"/scenes/{SeededSceneId}"];
        yield return [HttpMethod.Post, "/scenes/"];
        yield return [HttpMethod.Put, $"/scenes/{SeededSceneId}"];
        yield return [HttpMethod.Delete, $"/scenes/{SeededSceneId}"];
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (method == HttpMethod.Post || method == HttpMethod.Put)
            request.Content = JsonContent.Create(new Scene { Id = SeededSceneId, Name = "Scene" });

        return request;
    }

    #region Missing API key

    [Theory]
    [MemberData(nameof(ProtectedRequests))]
    public async Task InvokeAsync_ApiKeyHeaderMissing_ReturnsUnauthorized(
        HttpMethod method,
        string url
    )
    {
        // Arrange
        var client = factory.CreateClient();
        var request = BuildRequest(method, url);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Invalid API key

    [Theory]
    [MemberData(nameof(ProtectedRequests))]
    public async Task InvokeAsync_ApiKeyHeaderInvalid_ReturnsUnauthorized(
        HttpMethod method,
        string url
    )
    {
        // Arrange
        var client = factory.CreateAuthorizedClient("invalid-api-key");
        var request = BuildRequest(method, url);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Valid API key

    [Theory]
    [MemberData(nameof(ProtectedRequests))]
    public async Task InvokeAsync_ApiKeyHeaderValid_DoesNotReturnUnauthorized(
        HttpMethod method,
        string url
    )
    {
        // Arrange
        var client = factory.CreateAuthorizedClient();
        var request = BuildRequest(method, url);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Case sensitivity

    [Fact]
    public async Task InvokeAsync_ApiKeyHeaderNameHasDifferentCasing_DoesNotReturnUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/scenes/");
        request.Headers.Add("x-api-key", "dev-api-key");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
