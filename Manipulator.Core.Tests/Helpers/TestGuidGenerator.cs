using Manipulator.Core.IdGeneration;

namespace Manipulator.Core.Tests.Helpers;

public class TestGuidGenerator : IGuidGenerator
{
    private int _counter;

    public string Generate() => $"entity_{++_counter}";
}
