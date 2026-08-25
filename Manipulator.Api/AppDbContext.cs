using Microsoft.EntityFrameworkCore;

namespace Manipulator.Api;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Scene> Scenes => Set<Scene>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Scene>(scene =>
        {
            scene.HasKey(s => s.Id);
            scene.Property(s => s.Name).IsRequired().HasMaxLength(20);
            scene.Property(s => s.Created).IsRequired();
            scene.Property(s => s.Updated).IsRequired();
        });
    }
}
