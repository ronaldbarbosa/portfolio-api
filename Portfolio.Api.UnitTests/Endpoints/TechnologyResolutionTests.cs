using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.Endpoints;
using Portfolio.Api.Models;

namespace Portfolio.Api.UnitTests.Endpoints;

public class TechnologyResolutionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public TechnologyResolutionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ResolveTechnologiesAsync_ReturnsEmpty_WhenNamesIsNull()
    {
        var result = await ProjectEndpoints.ResolveTechnologiesAsync(_db, null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolveTechnologiesAsync_ReturnsEmpty_WhenNamesIsEmpty()
    {
        var result = await ProjectEndpoints.ResolveTechnologiesAsync(_db, []);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolveTechnologiesAsync_CreatesNewTechnologies()
    {
        var result = await ProjectEndpoints.ResolveTechnologiesAsync(_db, ["C#", "ASP.NET Core"]);
        await _db.SaveChangesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, await _db.Technologies.CountAsync());
        Assert.Contains(result, t => t.Name == "C#");
        Assert.Contains(result, t => t.Name == "ASP.NET Core");
    }

    [Fact]
    public async Task ResolveTechnologiesAsync_ReusesExistingTechnology_CaseInsensitive()
    {
        _db.Technologies.Add(new Technology { Id = Guid.NewGuid(), Name = "C#" });
        await _db.SaveChangesAsync();

        var result = await ProjectEndpoints.ResolveTechnologiesAsync(_db, ["c#"]);
        await _db.SaveChangesAsync();

        Assert.Single(result);
        Assert.Equal("C#", result[0].Name);
        Assert.Equal(1, await _db.Technologies.CountAsync());
    }

    [Fact]
    public async Task ResolveTechnologiesAsync_DeduplicatesCaseVariationsInInput()
    {
        var result = await ProjectEndpoints.ResolveTechnologiesAsync(_db, ["C#", "c#", " C# "]);
        await _db.SaveChangesAsync();

        Assert.Single(result);
        Assert.Equal(1, await _db.Technologies.CountAsync());
    }
}
