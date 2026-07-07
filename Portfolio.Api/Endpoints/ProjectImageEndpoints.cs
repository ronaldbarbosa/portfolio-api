using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.Dtos;
using Portfolio.Api.Models;
using Portfolio.Api.Services;

namespace Portfolio.Api.Endpoints;

public static class ProjectImageEndpoints
{
    public static RouteGroupBuilder MapProjectImageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/images")
            .WithTags("ProjectImages")
            .RequireAuthorization();

        group.MapPost("/", UploadAsync).DisableAntiforgery();
        group.MapDelete("/{imageId:guid}", DeleteAsync);
        group.MapPost("/{imageId:guid}/cover", SetCoverAsync);
        group.MapPut("/order", ReorderAsync);

        return group;
    }

    private static async Task<IResult> UploadAsync(
        Guid projectId,
        IFormFileCollection files,
        AppDbContext db,
        IImageStorageService imageStorage,
        CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            return Results.NotFound();
        }

        if (files.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["files"] = ["Envie ao menos um arquivo de imagem."],
            });
        }

        var errors = new Dictionary<string, string[]>();
        foreach (var file in files)
        {
            if (!imageStorage.IsValid(file, out var error))
            {
                errors[file.FileName] = [error!];
            }
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var nextOrder = project.Images.Count == 0 ? 0 : project.Images.Max(i => i.Order) + 1;
        var hasCover = project.Images.Any(i => i.IsCover);

        var createdImages = new List<ProjectImage>();
        foreach (var file in files)
        {
            var saved = await imageStorage.SaveAsync(projectId, file, cancellationToken);
            createdImages.Add(new ProjectImage
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                FileName = saved.FileName,
                Url = saved.Url,
                Order = nextOrder++,
                IsCover = !hasCover && createdImages.Count == 0,
            });
        }

        db.ProjectImages.AddRange(createdImages);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/projects/{projectId}",
            createdImages.Select(i => new ProjectImageResponse(i.Id, i.Url, i.IsCover, i.Order)));
    }

    private static async Task<IResult> DeleteAsync(
        Guid projectId,
        Guid imageId,
        AppDbContext db,
        IImageStorageService imageStorage,
        CancellationToken cancellationToken)
    {
        var image = await db.ProjectImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProjectId == projectId, cancellationToken);

        if (image is null)
        {
            return Results.NotFound();
        }

        imageStorage.Delete(projectId, image.FileName);
        db.ProjectImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> SetCoverAsync(
        Guid projectId,
        Guid imageId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var images = await db.ProjectImages
            .Where(i => i.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        if (images.All(i => i.Id != imageId))
        {
            return Results.NotFound();
        }

        foreach (var image in images)
        {
            image.IsCover = image.Id == imageId;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(images
            .OrderBy(i => i.Order)
            .Select(i => new ProjectImageResponse(i.Id, i.Url, i.IsCover, i.Order)));
    }

    private static async Task<IResult> ReorderAsync(
        Guid projectId,
        SetImageOrderRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var images = await db.ProjectImages
            .Where(i => i.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var requestedIds = request.ImageIds;
        if (requestedIds is null || images.Count != requestedIds.Count ||
            !images.Select(i => i.Id).ToHashSet().SetEquals(requestedIds))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["imageIds"] = ["A lista deve conter exatamente os ids das imagens existentes do projeto."],
            });
        }

        for (var index = 0; index < requestedIds.Count; index++)
        {
            images.First(i => i.Id == requestedIds[index]).Order = index;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(images
            .OrderBy(i => i.Order)
            .Select(i => new ProjectImageResponse(i.Id, i.Url, i.IsCover, i.Order)));
    }
}
