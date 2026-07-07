using Portfolio.Api.Dtos;
using Portfolio.Api.Models;

namespace Portfolio.Api.UnitTests.Dtos;

public class ProjectMappingExtensionsTests
{
    [Fact]
    public void ToResponse_MapsScalarFields()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Portfolio API",
            Description = "Descrição",
            RepositoryUrl = "https://github.com/user/repo",
            DemoUrl = "https://demo.dev",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };

        var response = project.ToResponse();

        Assert.Equal(project.Id, response.Id);
        Assert.Equal(project.Name, response.Name);
        Assert.Equal(project.Description, response.Description);
        Assert.Equal(project.RepositoryUrl, response.RepositoryUrl);
        Assert.Equal(project.DemoUrl, response.DemoUrl);
        Assert.Equal(project.CreatedAt, response.CreatedAt);
        Assert.Equal(project.UpdatedAt, response.UpdatedAt);
    }

    [Fact]
    public void ToResponse_OrdersTechnologiesAlphabetically()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Portfolio API",
            Description = "Descrição",
            Technologies =
            [
                new Technology { Id = Guid.NewGuid(), Name = "Zeta" },
                new Technology { Id = Guid.NewGuid(), Name = "Alpha" },
            ],
        };

        var response = project.ToResponse();

        Assert.Equal(["Alpha", "Zeta"], response.Technologies);
    }

    [Fact]
    public void ToResponse_OrdersImagesByOrder()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Portfolio API",
            Description = "Descrição",
            Images =
            [
                new ProjectImage { Id = Guid.NewGuid(), FileName = "b.png", Url = "/b.png", Order = 1 },
                new ProjectImage { Id = Guid.NewGuid(), FileName = "a.png", Url = "/a.png", Order = 0 },
            ],
        };

        var response = project.ToResponse();

        Assert.Equal(["/a.png", "/b.png"], response.Images.Select(i => i.Url));
    }
}
