namespace Portfolio.Api.Models;

public class Technology
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
