using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Portfolio.Api.Dtos;

namespace Portfolio.Api.IntegrationTests;

public class ProjectImageEndpointsTests(PortfolioApiFactory factory) : IClassFixture<PortfolioApiFactory>
{
    private static readonly byte[] FakePngBytes = "fake-png-bytes"u8.ToArray();

    private static MultipartFormDataContent BuildImageContent(
        byte[]? bytes = null, string contentType = "image/png", string fileName = "photo.png")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes ?? FakePngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "files", fileName);
        return content;
    }

    private async Task<(HttpClient Client, Guid ProjectId)> CreateProjectAsync()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var request = new CreateProjectRequest(
            Name: $"Project {Guid.NewGuid()}",
            Description: "Descrição",
            RepositoryUrl: null,
            DemoUrl: null,
            IsFinished: false,
            FrontendTechnologies: [],
            BackendTechnologies: [],
            Tools: []);

        var response = await client.PostAsJsonAsync("/api/projects", request);
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        return (client, project!.Id);
    }

    [Fact]
    public async Task Upload_ReturnsCreated_AndFirstImageIsCover()
    {
        var (client, projectId) = await CreateProjectAsync();

        var response = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var images = await response.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        Assert.Single(images!);
        Assert.True(images![0].IsCover);

        var expectedPath = Path.Combine(
            factory.ContentRoot, "wwwroot", "uploads", "projects", projectId.ToString(),
            Path.GetFileName(images[0].Url));
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task Upload_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync($"/api/projects/{Guid.NewGuid()}/images", BuildImageContent());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutToken_ReturnsUnauthorized()
    {
        var (_, projectId) = await CreateProjectAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithUnsupportedContentType_ReturnsValidationProblem()
    {
        var (client, projectId) = await CreateProjectAsync();

        var response = await client.PostAsync(
            $"/api/projects/{projectId}/images",
            BuildImageContent(contentType: "text/plain", fileName: "notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadedImage_IsServedAsStaticFile()
    {
        var (client, projectId) = await CreateProjectAsync();
        var uploadResponse = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var images = await uploadResponse.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();

        var imageResponse = await client.GetAsync(images![0].Url);

        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.Equal("image/png", imageResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Delete_RemovesImageFile()
    {
        var (client, projectId) = await CreateProjectAsync();
        var uploadResponse = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var images = await uploadResponse.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var imagePath = Path.Combine(
            factory.ContentRoot, "wwwroot", "uploads", "projects", projectId.ToString(),
            Path.GetFileName(images![0].Url));
        Assert.True(File.Exists(imagePath));

        var deleteResponse = await client.DeleteAsync($"/api/projects/{projectId}/images/{images[0].Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.False(File.Exists(imagePath));
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        var (client, projectId) = await CreateProjectAsync();
        var uploadResponse = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var images = await uploadResponse.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.DeleteAsync($"/api/projects/{projectId}/images/{images![0].Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeletingProject_RemovesImageFilesToo()
    {
        var (client, projectId) = await CreateProjectAsync();
        var uploadResponse = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var images = await uploadResponse.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var imagePath = Path.Combine(
            factory.ContentRoot, "wwwroot", "uploads", "projects", projectId.ToString(),
            Path.GetFileName(images![0].Url));

        var deleteResponse = await client.DeleteAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.False(File.Exists(imagePath));
    }

    [Fact]
    public async Task SetCover_MakesTargetCoverAndUnsetsPrevious()
    {
        var (client, projectId) = await CreateProjectAsync();
        var firstUpload = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var firstImages = await firstUpload.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var secondUpload = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var secondImages = await secondUpload.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var newCoverId = secondImages![0].Id;

        var response = await client.PostAsync($"/api/projects/{projectId}/images/{newCoverId}/cover", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var images = await response.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        Assert.True(images!.Single(i => i.Id == newCoverId).IsCover);
        Assert.False(images!.Single(i => i.Id == firstImages![0].Id).IsCover);
    }

    [Fact]
    public async Task SetCover_ReturnsNotFound_WhenImageDoesNotBelongToProject()
    {
        var (client, projectId) = await CreateProjectAsync();

        var response = await client.PostAsync($"/api/projects/{projectId}/images/{Guid.NewGuid()}/cover", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetCover_WithoutToken_ReturnsUnauthorized()
    {
        var (client, projectId) = await CreateProjectAsync();
        var uploadResponse = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var images = await uploadResponse.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.PostAsync($"/api/projects/{projectId}/images/{images![0].Id}/cover", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reorder_UpdatesOrderSequentially()
    {
        var (client, projectId) = await CreateProjectAsync();
        var firstUpload = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var firstImages = await firstUpload.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var secondUpload = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var secondImages = await secondUpload.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var firstId = firstImages![0].Id;
        var secondId = secondImages![0].Id;

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/images/order",
            new SetImageOrderRequest([secondId, firstId]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var images = await response.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        Assert.Equal(secondId, images![0].Id);
        Assert.Equal(0, images[0].Order);
        Assert.Equal(firstId, images[1].Id);
        Assert.Equal(1, images[1].Order);
    }

    [Fact]
    public async Task Reorder_WithMismatchedIds_ReturnsValidationProblem()
    {
        var (client, projectId) = await CreateProjectAsync();
        var uploadResponse = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var images = await uploadResponse.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/images/order",
            new SetImageOrderRequest([images![0].Id, Guid.NewGuid()]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reorder_WithoutToken_ReturnsUnauthorized()
    {
        var (client, projectId) = await CreateProjectAsync();
        var uploadResponse = await client.PostAsync($"/api/projects/{projectId}/images", BuildImageContent());
        var images = await uploadResponse.Content.ReadFromJsonAsync<List<ProjectImageResponse>>();
        var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.PutAsJsonAsync(
            $"/api/projects/{projectId}/images/order",
            new SetImageOrderRequest([images![0].Id]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
