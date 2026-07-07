using Portfolio.Api.Models;

namespace Portfolio.Api.Dtos;

public record ProjectImageResponse(Guid Id, string Url, bool IsCover, int Order);

public record ProjectResponse(
    Guid Id,
    string Name,
    string Description,
    string? RepositoryUrl,
    string? DemoUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<ProjectImageResponse> Images);

public record CreateProjectRequest(
    string Name,
    string Description,
    string? RepositoryUrl,
    string? DemoUrl,
    List<string>? Technologies);

public record UpdateProjectRequest(
    string Name,
    string Description,
    string? RepositoryUrl,
    string? DemoUrl,
    List<string>? Technologies);

public static class ProjectMappingExtensions
{
    public static ProjectResponse ToResponse(this Project project) => new(
        project.Id,
        project.Name,
        project.Description,
        project.RepositoryUrl,
        project.DemoUrl,
        project.CreatedAt,
        project.UpdatedAt,
        project.Technologies.Select(t => t.Name).OrderBy(n => n).ToList(),
        project.Images
            .OrderBy(i => i.Order)
            .Select(i => new ProjectImageResponse(i.Id, i.Url, i.IsCover, i.Order))
            .ToList());
}
