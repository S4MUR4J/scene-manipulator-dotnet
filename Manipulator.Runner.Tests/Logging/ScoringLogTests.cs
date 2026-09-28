using FluentAssertions;
using Manipulator.Runner.Logging;
using Manipulator.Scenarios.Scoring;

namespace Manipulator.Runner.Tests.Logging;

public class ScoringLogTests
{
    [Fact]
    public void From_MapsCoverageSuccessAndEveryRequirementResult()
    {
        // Arrange
        var scoringResult = ScoringResult.From([
            new RequirementResult("R1", Passed: true, Reason: "ok"),
            new RequirementResult("R2", Passed: false, Reason: "missing entity"),
        ]);

        // Act
        var log = ScoringLog.From(scoringResult);

        // Assert
        log.Coverage.Should().Be(0.5);
        log.Success.Should().BeFalse();
        log.Requirements.Should()
            .BeEquivalentTo([
                new RequirementResultLog("R1", Passed: true, Reason: "ok"),
                new RequirementResultLog("R2", Passed: false, Reason: "missing entity"),
            ]);
    }
}
