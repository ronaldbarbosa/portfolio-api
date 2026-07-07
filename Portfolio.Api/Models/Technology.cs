namespace Portfolio.Api.Models;

public enum TechnologyCategory
{
    Frontend,
    Backend,
    Tool,
}

public class Technology
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public TechnologyCategory Category { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
