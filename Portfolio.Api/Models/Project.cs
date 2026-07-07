namespace Portfolio.Api.Models;

public class Project
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? DemoUrl { get; set; }
    public bool IsFinished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Technology> Technologies { get; set; } = new List<Technology>();
    public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
}
