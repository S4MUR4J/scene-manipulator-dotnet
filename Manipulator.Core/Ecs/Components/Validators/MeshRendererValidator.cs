using FluentValidation;

namespace Manipulator.Core.Ecs.Components.Validators;

public class MeshRendererValidator : AbstractValidator<MeshRenderer>
{
    public MeshRendererValidator()
    {
        RuleFor(mr => mr.Opacity).InclusiveBetween(0f, 1f);
        RuleFor(mr => mr.Metalness).InclusiveBetween(0f, 1f);
        RuleFor(mr => mr.Roughness).InclusiveBetween(0f, 1f);
        RuleFor(mr => mr.Color).NotEmpty();
    }
}