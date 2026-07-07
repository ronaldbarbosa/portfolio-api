using Portfolio.Api.Endpoints;

namespace Portfolio.Api.UnitTests.Endpoints;

public class ProjectValidationTests
{
    [Fact]
    public void ValidateProject_ReturnsNoErrors_ForValidInput()
    {
        var errors = ProjectEndpoints.ValidateProject(
            "Portfolio API", "Descrição válida", "https://github.com/user/repo", "https://demo.dev");

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateProject_ReturnsNoErrors_WhenOptionalUrlsAreNull()
    {
        var errors = ProjectEndpoints.ValidateProject("Portfolio API", "Descrição válida", null, null);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateProject_RequiresName(string name)
    {
        var errors = ProjectEndpoints.ValidateProject(name, "Descrição válida", null, null);

        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void ValidateProject_RejectsNameAboveMaxLength()
    {
        var errors = ProjectEndpoints.ValidateProject(new string('a', 201), "Descrição válida", null, null);

        Assert.True(errors.ContainsKey("name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateProject_RequiresDescription(string description)
    {
        var errors = ProjectEndpoints.ValidateProject("Portfolio API", description, null, null);

        Assert.True(errors.ContainsKey("description"));
    }

    [Fact]
    public void ValidateProject_RejectsDescriptionAboveMaxLength()
    {
        var errors = ProjectEndpoints.ValidateProject("Portfolio API", new string('a', 4001), null, null);

        Assert.True(errors.ContainsKey("description"));
    }

    [Fact]
    public void ValidateProject_RejectsInvalidRepositoryUrl()
    {
        var errors = ProjectEndpoints.ValidateProject("Portfolio API", "Descrição válida", "not-a-url", null);

        Assert.True(errors.ContainsKey("repositoryUrl"));
    }

    [Fact]
    public void ValidateProject_RejectsInvalidDemoUrl()
    {
        var errors = ProjectEndpoints.ValidateProject("Portfolio API", "Descrição válida", null, "not-a-url");

        Assert.True(errors.ContainsKey("demoUrl"));
    }
}
