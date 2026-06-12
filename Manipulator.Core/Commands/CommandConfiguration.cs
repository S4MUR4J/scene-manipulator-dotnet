using Manipulator.Core.Commands.Validation;
using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands;

record CommandConfiguration(
    Func<Scene, ICommand, CommandResult> Handler,
    IReadOnlyList<ICommandValidator> Validators
);
