using Manipulator.Scenarios.Specs;

namespace Manipulator.Scenarios.Contracts;

record RunConfigDto(int? Main, int? PerVariant)
{
    public RunConfig ToDomain() => new RunConfig(Main!.Value, PerVariant!.Value);
}
