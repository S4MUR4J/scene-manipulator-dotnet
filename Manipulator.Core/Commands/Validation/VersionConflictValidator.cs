using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands.Validation;

public class VersionConflictValidator : ICommandValidator
{
    public ValidationResult Validate(Scene scene, ICommand command)
    {
        if (!command.ExpectedVersion.HasValue)
            return ValidationResult.Ok();

        return command.ExpectedVersion.Value == scene.Version
            ? ValidationResult.Ok()
            : ValidationResult.Fail(
                $"Version conflict: expected {command.ExpectedVersion.Value}, current is {scene.Version}."
            );
    }
}
