using FluentValidation;

namespace Manipulator.Api.Domain;

public class SceneValidator : AbstractValidator<Scene>
{
    public SceneValidator()
    {
        RuleFor(s => s.Name).NotEmpty().MaximumLength(20);
    }
}
