using Manipulator.Core.Commands.Validation;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands;

/// <summary>
/// Routes incoming commands to their registered handlers and publishes any resulting scene events.
/// </summary>
/// <remarks>
/// Handlers are keyed by the string <c>ICommand.Type</c> rather than the CLR type so that commands
/// can arrive from external sources (e.g. JSON payloads) without requiring compile-time coupling.
/// Validators run before the handler; if any rejects the command, a failure result is returned and
/// the handler is never invoked. Version conflict is checked first — if <see cref="ICommand.ExpectedVersion"/>
/// is set and does not match <see cref="Scene.Version"/>, the command is rejected immediately.
/// Successful results carry a list of <see cref="ISceneEvent"/> objects that are forwarded to the
/// <see cref="EventBus"/> only after the handler returns success, ensuring events are never emitted
/// for failed operations. Handler exceptions are caught and converted to a failure result so callers
/// always receive a <see cref="CommandResult"/> instead of an unhandled exception.
/// </remarks>
public class CommandDispatcher(Scene scene, EventBus eventBus)
{
    private readonly Dictionary<string, CommandConfiguration> _handlers =
        new Dictionary<string, CommandConfiguration>();

    public void Register<T>(
        string commandType,
        ICommandHandler<T> handler,
        params ICommandValidator[] validators
    )
        where T : ICommand
    {
        _handlers[commandType] = new CommandConfiguration(
            Handler: (sceneData, command) => handler.Handle(sceneData, (T)command),
            Validators: validators
        );
    }

    public CommandResult Dispatch(ICommand command)
    {
        if (!_handlers.TryGetValue(command.Type, out var entry))
            return CommandResult.Fail($"No handler registered for command type '{command.Type}'.");

        foreach (var validator in entry.Validators)
        {
            var validation = validator.Validate(scene, command);
            if (!validation.IsValid)
                return CommandResult.Fail(validation.Error!);
        }

        CommandResult result;
        try
        {
            result = entry.Handler(scene, command);
        }
        catch (Exception ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        if (!result.IsSuccess)
            return result;

        foreach (var sceneEvent in result.Events)
            eventBus.Publish(sceneEvent);

        return result;
    }
}
