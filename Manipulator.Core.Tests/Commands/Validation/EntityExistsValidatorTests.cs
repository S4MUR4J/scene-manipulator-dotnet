using FluentAssertions;
using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Validation;
using Manipulator.Core.Tests.Helpers;

namespace Manipulator.Core.Tests.Commands.Validation;

public class EntityExistsValidatorTests
{
    private readonly EntityExistsValidator _validator = new EntityExistsValidator();

    [Fact]
    public void Validate_NonEntityTargetCommand_ReturnsValid()
    {
        var scene = new SceneBuilder().Build();
        var result = _validator.Validate(scene, new NonTargetCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EntityExists_ReturnsValid()
    {
        var scene = new SceneBuilder().WithEntity().Build();
        var result = _validator.Validate(scene, new TargetCommand(SceneBuilder.Id(1)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EntityMissing_ReturnsInvalid()
    {
        var scene = new SceneBuilder().Build();
        var result = _validator.Validate(scene, new TargetCommand(SceneBuilder.Id(99)));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EntityMissing_ErrorMentionsEntityId()
    {
        var scene = new SceneBuilder().Build();
        var result = _validator.Validate(scene, new TargetCommand(SceneBuilder.Id(99)));
        result.Error.Should().Contain(SceneBuilder.Id(99));
    }
}

file record NonTargetCommand : ICommand
{
    public string Type => "NonTarget";
    public long? ExpectedVersion => null;
}

file record TargetCommand(string EntityId) : IEntityTargetCommand
{
    public string Type => "Target";
    public long? ExpectedVersion => null;
}
