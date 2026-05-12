using Manipulator.Core.Ecs;
using Manipulator.Core.Events;

namespace Manipulator.Core.Commands;

/// <summary>
/// Routes incoming commands to their registered handlers and publishes any resulting scene events.
/// </summary>
/// <remarks>
/// Handlers are keyed by the string <c>ICommand.Type</c> rather than the CLR type so that commands
/// can arrive from external sources (e.g. JSON payloads) without requiring compile-time coupling.
/// Successful results carry a list of <see cref="ISceneEvent"/> objects that are forwarded to the
/// <see cref="EventBus"/> only after the handler returns success, ensuring events are never emitted
/// for failed operations. Handler exceptions are caught and converted to a failure result so callers
/// always receive a <see cref="CommandResult"/> instead of an unhandled exception.
/// </remarks>
public class CommandDispatcher(Scene scene, EventBus eventBus)
{
    private readonly Dictionary<string, Func<Scene, ICommand, CommandResult>> _handlers = new();

    public void Register<T>(string commandType, ICommandHandler<T> handler) where T : ICommand
    {
        _handlers[commandType] = (sceneData, command) => handler.Handle(sceneData, (T)command);
    }

    public CommandResult Dispatch(ICommand command)
    {
        if (!_handlers.TryGetValue(command.Type, out var handlerFunc))
            return CommandResult.Fail($"No handler registered for command type '{command.Type}'.");

        CommandResult result;
        try
        {
            result = handlerFunc(scene, command);
        }
        catch (Exception ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        if (!result.IsSuccess) return result;
        
        foreach (var sceneEvent in result.Events)
            eventBus.Publish(sceneEvent);

        return result;
    }
}
