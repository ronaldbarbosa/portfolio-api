using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Models;

namespace Portfolio.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).IsRequired().HasMaxLength(4000);
            entity.Property(p => p.RepositoryUrl).HasMaxLength(2048);
            entity.Property(p => p.DemoUrl).HasMaxLength(2048);

            entity.HasMany(p => p.Images)
                .WithOne(i => i.Project)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Technologies)
                .WithMany(t => t.Projects)
                .UsingEntity(j => j.ToTable("ProjectTechnologies"));
        });

        builder.Entity<Technology>(entity =>
        {
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        builder.Entity<ProjectImage>(entity =>
        {
            entity.Property(i => i.FileName).IsRequired().HasMaxLength(300);
            entity.Property(i => i.Url).IsRequired().HasMaxLength(2048);
        });
    }
}
