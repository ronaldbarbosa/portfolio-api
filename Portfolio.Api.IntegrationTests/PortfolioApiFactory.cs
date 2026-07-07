using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portfolio.Api.Data;
using Portfolio.Api.Dtos;

namespace Portfolio.Api.IntegrationTests;

public class PortfolioApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@integration.test";
    public const string AdminPassword = "Integration-Test!123";

    private readonly string _contentRoot =
        Path.Combine(Path.GetTempPath(), "portfolio-integration-tests-" + Guid.NewGuid());

    private SqliteConnection? _connection;

    public string ContentRoot => _contentRoot;

    public PortfolioApiFactory()
    {
        // Program.cs lê Configuration de forma eager (antes do WebApplicationBuilder.Build()),
        // então overrides via ConfigureAppConfiguration/ConfigureWebHost chegam tarde demais.
        // Variáveis de ambiente já estão disponíveis desde WebApplication.CreateBuilder(args).
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Data Source=unused-overridden-in-tests");
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-test-signing-key-with-enough-length-1234567890");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Portfolio.Api.IntegrationTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Portfolio.Api.IntegrationTests");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("AdminUser__Email", AdminEmail);
        Environment.SetEnvironmentVariable("AdminUser__Password", AdminPassword);
        Environment.SetEnvironmentVariable("DevUser__Email", "dev@integration.test");
        Environment.SetEnvironmentVariable("DevUser__Password", "Integration-Test-Dev!123");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_contentRoot);
        builder.UseContentRoot(_contentRoot);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task<string> GetAdminTokenAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(AdminEmail, AdminPassword));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.Token;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var token = await GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        _connection?.Close();
        _connection?.Dispose();

        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }
}
