namespace Portfolio.Api.Services;

public class LocalImageStorageService(IWebHostEnvironment environment) : IImageStorageService
{
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private string UploadsRoot => Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "projects");

    public bool IsValid(IFormFile file, out string? error)
    {
        if (file.Length == 0)
        {
            error = "O arquivo está vazio.";
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            error = "O arquivo excede o tamanho máximo de 5MB.";
            return false;
        }

        if (!AllowedContentTypes.ContainsKey(file.ContentType))
        {
            error = "Tipo de arquivo não suportado. Use JPEG, PNG, WEBP ou GIF.";
            return false;
        }

        error = null;
        return true;
    }

    public async Task<SavedImage> SaveAsync(Guid projectId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = AllowedContentTypes[file.ContentType];
        var fileName = $"{Guid.NewGuid()}{extension}";
        var projectDirectory = Path.Combine(UploadsRoot, projectId.ToString());
        Directory.CreateDirectory(projectDirectory);

        var filePath = Path.Combine(projectDirectory, fileName);
        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"/uploads/projects/{projectId}/{fileName}";
        return new SavedImage(fileName, url);
    }

    public void Delete(Guid projectId, string fileName)
    {
        var filePath = Path.Combine(UploadsRoot, projectId.ToString(), fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
