using Manipulator.Core.Commands;

namespace Manipulator.Core.Results;

/// <summary>
/// One response shape for every adapter (MCP tools today, the REST API later): a payload, an
/// HTTP-style status code, and an error message when the code isn't a success. Callers decide what
/// <typeparamref name="T"/> and the code are; this type only carries them.
/// </summary>
public sealed record ManipulatorResult<T>(T? Result, int Code, string? Error = null)
{
    public static ManipulatorResult<T> Success(T? result, int code = 200)
    {
        return new ManipulatorResult<T>(result, code, null);
    }

    public static ManipulatorResult<T> Failure(string error, int code = 400)
    {
        return new ManipulatorResult<T>(default, code, error);
    }

    public static ManipulatorResult<T> FromCommandResult(
        CommandResult commandResult,
        T? result,
        int failureCode = 400
    )
    {
        return commandResult.IsSuccess
            ? Success(result)
            : Failure(commandResult.Error ?? "Command failed.", failureCode);
    }
}
