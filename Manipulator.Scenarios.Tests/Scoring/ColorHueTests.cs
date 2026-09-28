using FluentAssertions;
using Manipulator.Scenarios.Scoring;

namespace Manipulator.Scenarios.Tests.Scoring;

public class ColorHueTests
{
    [Theory]
    [InlineData("#ff0000", 0.0)]
    [InlineData("#00ff00", 120.0)]
    [InlineData("#0000ff", 240.0)]
    [InlineData("#ffff00", 60.0)]
    [InlineData("#ff00ff", 300.0)]
    public void FromHex_PrimaryAndSecondaryColors_ReturnsExpectedHue(string hex, double expectedHue)
    {
        // Act
        var hue = ColorHue.FromHex(hex);

        // Assert
        hue.Should().BeApproximately(expectedHue, 0.01);
    }

    [Theory]
    [InlineData("#ffffff")]
    [InlineData("#000000")]
    [InlineData("#808080")]
    public void FromHex_Greyscale_ReturnsZero(string hex)
    {
        // Act
        var hue = ColorHue.FromHex(hex);

        // Assert
        hue.Should().Be(0.0);
    }

    [Fact]
    public void FromHex_IgnoresLeadingHash()
    {
        // Act
        var withHash = ColorHue.FromHex("#ff0000");
        var withoutHash = ColorHue.FromHex("ff0000");

        // Assert
        withoutHash.Should().Be(withHash);
    }
}
