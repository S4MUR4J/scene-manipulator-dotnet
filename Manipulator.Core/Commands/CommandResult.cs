using Manipulator.Core.Events;

namespace Manipulator.Core.Commands;

public record CommandResult
{
    public bool IsSuccess { get; private init; }
    public IReadOnlyList<ISceneEvent> Events { get; private init; } = [];
    public string? Error { get; private init; }
    public object? Data { get; private init; }

    public static CommandResult Ok(IReadOnlyList<ISceneEvent> events, object? data = null) =>
        new CommandResult
        {
            IsSuccess = true,
            Events = events,
            Data = data,
        };

    public static CommandResult Fail(string error) => new CommandResult { IsSuccess = false, Error = error };
}
