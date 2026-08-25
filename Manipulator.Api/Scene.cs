namespace Manipulator.Api;

public class Scene
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
