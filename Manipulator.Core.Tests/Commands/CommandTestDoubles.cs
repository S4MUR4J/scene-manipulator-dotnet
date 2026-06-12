using Manipulator.Core.Commands;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;

namespace Manipulator.Core.Tests.Commands;

record FakeCommand(long? ExpectedVersion = null) : ICommand
{
    public string Type => "Fake";
}

record FakeEvent : ISceneEvent;

class ThrowingHandler : ICommandHandler<FakeCommand>
{
    public CommandResult Handle(Scene scene, FakeCommand command) =>
        throw new InvalidOperationException("handler error");
}

class SuccessHandler : ICommandHandler<FakeCommand>
{
    public CommandResult Handle(Scene scene, FakeCommand command) =>
        CommandResult.Ok([new FakeEvent()]);
}

class FailHandler : ICommandHandler<FakeCommand>
{
    public CommandResult Handle(Scene scene, FakeCommand command) =>
        CommandResult.Fail("handler failure");
}
