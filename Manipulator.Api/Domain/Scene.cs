namespace Manipulator.Api.Domain;

public class Scene
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string? Name { get; set; }

    public DateTime Created { get; init; } = DateTime.UtcNow;

    public DateTime Updated { get; set; } = DateTime.UtcNow;

    public string? Content { get; set; } = string.Empty;
}
