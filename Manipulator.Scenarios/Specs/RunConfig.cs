namespace Manipulator.Scenarios.Specs;

/// <summary>Number of runs (N) per prompt: the fixed main prompt and each of its variants.</summary>
public sealed record RunConfig(int Main, int PerVariant);
