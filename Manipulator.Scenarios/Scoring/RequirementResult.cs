namespace Manipulator.Scenarios.Scoring;

/// <summary>Outcome of scoring one <see cref="Specs.Requirement"/> against a scene.</summary>
public sealed record RequirementResult(string RequirementId, bool Passed, string Reason);
