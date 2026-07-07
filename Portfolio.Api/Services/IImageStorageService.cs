namespace Portfolio.Api.Services;

public record SavedImage(string FileName, string Url);

public interface IImageStorageService
{
    bool IsValid(IFormFile file, out string? error);
    Task<SavedImage> SaveAsync(Guid projectId, IFormFile file, CancellationToken cancellationToken = default);
    void Delete(Guid projectId, string fileName);
}
