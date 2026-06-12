using Manipulator.Core.Ecs;

namespace Manipulator.Core.Commands.Validation;

public interface ICommandValidator
{
    ValidationResult Validate(Scene scene, ICommand command);
}
