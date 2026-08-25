namespace Manipulator.Api.Domain;

public class Scene
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = null!;

    public DateTime Created { get; init; } = DateTime.UtcNow;

    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
