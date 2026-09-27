namespace Manipulator.Runner.Execution;

sealed record LoopResult(StopReason StopReason, string? FatalError = null);
