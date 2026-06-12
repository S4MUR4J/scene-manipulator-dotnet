namespace Manipulator.Core.Commands;

public interface ICommand
{
    string Type { get; }
    long? ExpectedVersion { get; }
}
