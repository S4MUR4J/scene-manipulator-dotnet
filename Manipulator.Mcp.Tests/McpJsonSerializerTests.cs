using FluentAssertions;
using Manipulator.Core.Results;
using Manipulator.Mcp.Tools;

namespace Manipulator.Mcp.Tests;

public class McpJsonSerializerTests
{
    #region Success

    [Fact]
    public void Serialize_SuccessResult_ProducesResultAndCodeWithNullError()
    {
        // Arrange
        var result = ManipulatorResult<string?>.Success("entity_1");

        // Act
        var json = McpJsonSerializer.Serialize(result);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(200);
        node["error"].Should().BeNull();
        node["result"]!.GetValue<string>().Should().Be("entity_1");
    }

    #endregion

    #region Failure

    [Fact]
    public void Serialize_FailureResult_ProducesNullResultWithErrorAndCode()
    {
        // Arrange
        var result = ManipulatorResult<string?>.Failure("Entity 'x' does not exist.", code: 404);

        // Act
        var json = McpJsonSerializer.Serialize(result);

        // Assert
        var node = ToolResponse.Parse(json);
        node["code"]!.GetValue<int>().Should().Be(404);
        node["result"].Should().BeNull();
        node["error"]!.GetValue<string>().Should().Be("Entity 'x' does not exist.");
    }

    #endregion

    #region Relaxed escaping

    [Fact]
    public void Serialize_ErrorContainsApostrophe_DoesNotEscapeIt()
    {
        // Arrange
        var result = ManipulatorResult<string?>.Failure("Entity 'x' does not exist.");

        // Act
        var json = McpJsonSerializer.Serialize(result);

        // Assert
        json.Should().Contain("Entity 'x' does not exist.");
        json.Should().NotContain("\\u0027");
    }

    #endregion
}
