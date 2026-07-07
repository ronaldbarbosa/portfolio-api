using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Api.Data;
using Portfolio.Api.Dtos;

namespace Portfolio.Api.IntegrationTests;

public class ProjectEndpointsTests(PortfolioApiFactory factory) : IClassFixture<PortfolioApiFactory>
{
    private static CreateProjectRequest ValidProjectRequest(
        string? name = null, List<string>? backendTechnologies = null) => new(
        Name: name ?? $"Project {Guid.NewGuid()}",
        Description: "Descrição de teste",
        RepositoryUrl: "https://github.com/user/repo",
        DemoUrl: "https://demo.dev",
        IsFinished: false,
        FrontendTechnologies: [],
        BackendTechnologies: backendTechnologies ?? ["C#", "ASP.NET Core"],
        Tools: []);

    [Fact]
    public async Task GetAll_ReturnsOk_WithoutAuthentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsPaginatedResponse_RespectingPageSize()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/projects", ValidProjectRequest());
        }

        var response = await client.GetAsync("/api/projects?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResponse<ProjectResponse>>();
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(1, page.PageNumber);
        Assert.True(page.HasNextPage);
        Assert.False(page.HasPreviousPage);
    }

    [Fact]
    public async Task Post_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects", ValidProjectRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithValidBody_ReturnsCreatedWithTechnologies()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/projects", ValidProjectRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal(["ASP.NET Core", "C#"], project!.BackendTechnologies);
    }

    [Fact]
    public async Task Post_WithInvalidBody_ReturnsValidationProblem()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/projects", ValidProjectRequest(name: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ReusesExistingTechnology_CaseInsensitive()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var technologyName = $"Tech-{Guid.NewGuid()}";

        await client.PostAsJsonAsync("/api/projects", ValidProjectRequest(backendTechnologies: [technologyName]));
        await client.PostAsJsonAsync(
            "/api/projects", ValidProjectRequest(backendTechnologies: [technologyName.ToUpperInvariant()]));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Technologies.CountAsync(t => t.Name.ToLower() == technologyName.ToLower());

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetById_ReturnsProject_WhenExists()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/projects", ValidProjectRequest()))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var response = await client.GetAsync($"/api/projects/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal(created.Id, project!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/projects/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesProject_ReplacesTechnologies()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/projects", ValidProjectRequest()))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var updateRequest = new UpdateProjectRequest(
            Name: "Updated Name",
            Description: "Nova descrição",
            RepositoryUrl: "https://github.com/user/repo2",
            DemoUrl: null,
            IsFinished: true,
            FrontendTechnologies: [],
            BackendTechnologies: [],
            Tools: ["Docker"]);

        var response = await client.PutAsJsonAsync($"/api/projects/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal("Updated Name", updated!.Name);
        Assert.True(updated.IsFinished);
        Assert.Equal(["Docker"], updated.Tools);
    }

    [Fact]
    public async Task Put_ReturnsNotFound_WhenMissing()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{Guid.NewGuid()}",
            new UpdateProjectRequest("Name", "Descrição", null, null, false, [], [], []));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesProject_AndSubsequentGetReturnsNotFound()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/projects", ValidProjectRequest()))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/projects/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/projects/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
