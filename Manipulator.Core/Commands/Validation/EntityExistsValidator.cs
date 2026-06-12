using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands.Validation;

public class EntityExistsValidator : ICommandValidator
{
    public ValidationResult Validate(Scene scene, ICommand command)
    {
        if (command is not IEntityTargetCommand entityCommand)
            return ValidationResult.Ok();

        return scene.HasEntity(entityCommand.EntityId)
            ? ValidationResult.Ok()
            : ValidationResult.Fail($"Entity '{entityCommand.EntityId}' does not exist.");
    }
}
