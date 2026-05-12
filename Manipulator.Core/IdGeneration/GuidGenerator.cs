namespace Manipulator.Core.IdGeneration;

public class GuidGenerator : IGuidGenerator
{
    public string Generate() => Guid.NewGuid().ToString("N")[..8];
}
