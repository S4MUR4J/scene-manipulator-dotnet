namespace Manipulator.Scenarios.Scoring;

/// <summary>
/// Aggregate score for a scene against a scenario spec's requirements. Coverage is the
/// satisfied/total ratio; Success requires every requirement to pass.
/// </summary>
public sealed record ScoringResult(
    IReadOnlyList<RequirementResult> Requirements,
    double Coverage,
    bool Success
)
{
    public static ScoringResult From(IReadOnlyList<RequirementResult> requirements)
    {
        var satisfied = requirements.Count(r => r.Passed);
        return new ScoringResult(
            requirements,
            Coverage: (double)satisfied / requirements.Count,
            Success: satisfied == requirements.Count
        );
    }
}
