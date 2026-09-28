using FluentAssertions;
using Manipulator.Scenarios.Scoring;

namespace Manipulator.Scenarios.Tests.Scoring;

public class ScoringResultTests
{
    [Fact]
    public void From_AllPassed_IsFullCoverageAndSuccess()
    {
        // Arrange
        var requirements = new[]
        {
            new RequirementResult("r1", Passed: true, Reason: "ok"),
            new RequirementResult("r2", Passed: true, Reason: "ok"),
        };

        // Act
        var result = ScoringResult.From(requirements);

        // Assert
        result.Coverage.Should().Be(1.0);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void From_PartialPass_ComputesCoverageRatio_AndFails()
    {
        // Arrange
        var requirements = new[]
        {
            new RequirementResult("r1", Passed: true, Reason: "ok"),
            new RequirementResult("r2", Passed: false, Reason: "missing entity"),
            new RequirementResult("r3", Passed: true, Reason: "ok"),
        };

        // Act
        var result = ScoringResult.From(requirements);

        // Assert
        result.Coverage.Should().BeApproximately(2.0 / 3.0, 1e-9);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void From_NonePassed_IsZeroCoverageAndFails()
    {
        // Arrange
        var requirements = new[]
        {
            new RequirementResult("r1", Passed: false, Reason: "missing entity"),
        };

        // Act
        var result = ScoringResult.From(requirements);

        // Assert
        result.Coverage.Should().Be(0.0);
        result.Success.Should().BeFalse();
    }
}
