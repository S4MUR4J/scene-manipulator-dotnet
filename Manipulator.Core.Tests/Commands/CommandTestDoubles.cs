using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;

namespace Manipulator.Core.Tests.Commands;

internal record FakeCommand : ICommand
{
    public string Type => "Fake";
}

internal record FakeEvent : ISceneEvent;

internal class ThrowingHandler : ICommandHandler<FakeCommand>
{
    public CommandResult Handle(Scene scene, FakeCommand command) =>
        throw new InvalidOperationException("handler error");
}

internal class SuccessHandler : ICommandHandler<FakeCommand>
{
    public CommandResult Handle(Scene scene, FakeCommand command) =>
        CommandResult.Ok([new FakeEvent()]);
}

internal class FailHandler : ICommandHandler<FakeCommand>
{
    public CommandResult Handle(Scene scene, FakeCommand command) =>
        CommandResult.Fail("handler failure");
}