using FluentValidation;

namespace Manipulator.Core.Ecs.Components.Validators;

public class MeshFilterValidator : AbstractValidator<MeshFilter>
{
    public MeshFilterValidator()
    {
        RuleFor(mf => mf.Geometry).IsInEnum();
    }
}
