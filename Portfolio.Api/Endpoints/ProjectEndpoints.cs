using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.Dtos;
using Portfolio.Api.Models;
using Portfolio.Api.Services;

namespace Portfolio.Api.Endpoints;

public static class ProjectEndpoints
{
    private const int DefaultPageSize = 9;

    public static RouteGroupBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync).RequireAuthorization();
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization();
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetAllAsync(AppDbContext db, int page = 1, int pageSize = DefaultPageSize)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize < 1 ? DefaultPageSize : pageSize;

        var query = db.Projects
            .Include(p => p.Technologies)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt);

        var totalItemCount = await query.CountAsync();
        var totalPages = totalItemCount == 0 ? 0 : (int)Math.Ceiling(totalItemCount / (double)pageSize);

        var projects = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new PaginatedResponse<ProjectResponse>(
            projects.Select(p => p.ToResponse()).ToList(),
            totalItemCount,
            page,
            pageSize,
            totalPages,
            page > 1,
            page < totalPages);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(Guid id, AppDbContext db)
    {
        var project = await db.Projects
            .Include(p => p.Technologies)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        return project is null ? Results.NotFound() : Results.Ok(project.ToResponse());
    }

    private static async Task<IResult> CreateAsync(CreateProjectRequest request, AppDbContext db)
    {
        var errors = ValidateProject(request.Name, request.Description, request.RepositoryUrl, request.DemoUrl);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            RepositoryUrl = NormalizeUrl(request.RepositoryUrl),
            DemoUrl = NormalizeUrl(request.DemoUrl),
            IsFinished = request.IsFinished,
            Technologies = await ResolveTechnologiesAsync(
                db, request.FrontendTechnologies, request.BackendTechnologies, request.Tools),
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return Results.Created($"/api/projects/{project.Id}", project.ToResponse());
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateProjectRequest request, AppDbContext db)
    {
        var errors = ValidateProject(request.Name, request.Description, request.RepositoryUrl, request.DemoUrl);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var project = await db.Projects
            .Include(p => p.Technologies)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null)
        {
            return Results.NotFound();
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description.Trim();
        project.RepositoryUrl = NormalizeUrl(request.RepositoryUrl);
        project.DemoUrl = NormalizeUrl(request.DemoUrl);
        project.IsFinished = request.IsFinished;
        project.UpdatedAt = DateTime.UtcNow;
        project.Technologies = await ResolveTechnologiesAsync(
            db, request.FrontendTechnologies, request.BackendTechnologies, request.Tools);

        await db.SaveChangesAsync();

        return Results.Ok(project.ToResponse());
    }

    private static async Task<IResult> DeleteAsync(Guid id, AppDbContext db, IImageStorageService imageStorage)
    {
        var project = await db.Projects
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null)
        {
            return Results.NotFound();
        }

        foreach (var image in project.Images)
        {
            imageStorage.Delete(project.Id, image.FileName);
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static string? NormalizeUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    internal static Dictionary<string, string[]> ValidateProject(
        string name, string description, string? repositoryUrl, string? demoUrl)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["O nome é obrigatório."];
        }
        else if (name.Length > 200)
        {
            errors["name"] = ["O nome deve ter no máximo 200 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors["description"] = ["A descrição é obrigatória."];
        }
        else if (description.Length > 4000)
        {
            errors["description"] = ["A descrição deve ter no máximo 4000 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(repositoryUrl) && !Uri.IsWellFormedUriString(repositoryUrl, UriKind.Absolute))
        {
            errors["repositoryUrl"] = ["A URL do repositório é inválida."];
        }

        if (!string.IsNullOrWhiteSpace(demoUrl) && !Uri.IsWellFormedUriString(demoUrl, UriKind.Absolute))
        {
            errors["demoUrl"] = ["A URL de demonstração é inválida."];
        }

        return errors;
    }

    internal static async Task<List<Technology>> ResolveTechnologiesAsync(
        AppDbContext db,
        List<string>? frontendNames,
        List<string>? backendNames,
        List<string>? toolNames)
    {
        // SQLite usa colação binária (case-sensitive) por padrão, então a comparação
        // case-insensitive precisa ser feita em memória em vez de traduzida para SQL.
        var existing = await db.Technologies.ToListAsync();

        var result = new List<Technology>();
        result.AddRange(ResolveCategory(db, existing, frontendNames, TechnologyCategory.Frontend));
        result.AddRange(ResolveCategory(db, existing, backendNames, TechnologyCategory.Backend));
        result.AddRange(ResolveCategory(db, existing, toolNames, TechnologyCategory.Tool));

        return result;
    }

    private static List<Technology> ResolveCategory(
        AppDbContext db, List<Technology> existing, List<string>? names, TechnologyCategory category)
    {
        if (names is null || names.Count == 0)
        {
            return [];
        }

        var normalized = names
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<Technology>();
        foreach (var name in normalized)
        {
            var match = existing.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                result.Add(match);
                continue;
            }

            var technology = new Technology { Id = Guid.NewGuid(), Name = name, Category = category };
            db.Technologies.Add(technology);
            existing.Add(technology);
            result.Add(technology);
        }

        return result;
    }
}
