using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Portfolio.Api.Services;

namespace Portfolio.Api.UnitTests.Services;

public class LocalImageStorageServiceTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly LocalImageStorageService _sut;

    public LocalImageStorageServiceTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "portfolio-unit-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_contentRoot);

        var environment = Substitute.For<IWebHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);

        _sut = new LocalImageStorageService(environment);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    private static IFormFile CreateFormFile(string contentType, long length, byte[]? content = null, string fileName = "photo.png")
    {
        content ??= new byte[length];
        var file = Substitute.For<IFormFile>();
        file.ContentType.Returns(contentType);
        file.FileName.Returns(fileName);
        file.Length.Returns(length);
        file.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Stream>().WriteAsync(content, 0, content.Length));
        return file;
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void IsValid_AcceptsSupportedContentTypes(string contentType)
    {
        var file = CreateFormFile(contentType, length: 1024);

        var isValid = _sut.IsValid(file, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void IsValid_RejectsEmptyFile()
    {
        var file = CreateFormFile("image/png", length: 0);

        var isValid = _sut.IsValid(file, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void IsValid_RejectsFileAboveMaxSize()
    {
        var file = CreateFormFile("image/png", length: 5 * 1024 * 1024 + 1);

        var isValid = _sut.IsValid(file, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void IsValid_RejectsUnsupportedContentType()
    {
        var file = CreateFormFile("text/plain", length: 1024);

        var isValid = _sut.IsValid(file, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task SaveAsync_WritesFileToDiskAndReturnsExpectedUrl()
    {
        var projectId = Guid.NewGuid();
        var content = "fake-image-bytes"u8.ToArray();
        var file = CreateFormFile("image/png", content.Length, content);

        var saved = await _sut.SaveAsync(projectId, file);

        Assert.EndsWith(".png", saved.FileName);
        Assert.Equal($"/uploads/projects/{projectId}/{saved.FileName}", saved.Url);

        var expectedPath = Path.Combine(_contentRoot, "wwwroot", "uploads", "projects", projectId.ToString(), saved.FileName);
        Assert.True(File.Exists(expectedPath));
        Assert.Equal(content, await File.ReadAllBytesAsync(expectedPath));
    }

    [Fact]
    public async Task Delete_RemovesExistingFile()
    {
        var projectId = Guid.NewGuid();
        var file = CreateFormFile("image/png", 4, [1, 2, 3, 4]);
        var saved = await _sut.SaveAsync(projectId, file);
        var path = Path.Combine(_contentRoot, "wwwroot", "uploads", "projects", projectId.ToString(), saved.FileName);
        Assert.True(File.Exists(path));

        _sut.Delete(projectId, saved.FileName);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Delete_DoesNotThrowWhenFileDoesNotExist()
    {
        var exception = Record.Exception(() => _sut.Delete(Guid.NewGuid(), "nonexistent.png"));

        Assert.Null(exception);
    }
}
