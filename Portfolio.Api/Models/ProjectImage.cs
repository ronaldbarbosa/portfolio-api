namespace Portfolio.Api.Models;

public class ProjectImage
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public required string FileName { get; set; }
    public required string Url { get; set; }
    public bool IsCover { get; set; }
    public int Order { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
