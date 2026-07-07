using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Data;
using Portfolio.Api.Endpoints;
using Portfolio.Api.Models;
using Portfolio.Api.OpenApi;
using Portfolio.Api.Options;
using Portfolio.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection(AdminSeedOptions.SectionName));
builder.Services.Configure<DevUserSeedOptions>(builder.Configuration.GetSection(DevUserSeedOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<AppDbContext>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer não configurada.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience não configurada.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

const string WebCorsPolicy = "Web";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IImageStorageService, LocalImageStorageService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecurityOperationTransformer>();
});

var app = builder.Build();

var webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "projects"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await AdminSeeder.SeedAsync(scope.ServiceProvider);

    if (app.Environment.IsDevelopment())
    {
        await DevUserSeeder.SeedAsync(scope.ServiceProvider);
    }
}

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

// PhysicalFileProvider explícito: env.WebRootFileProvider é fixado em Build() e vira
// NullFileProvider se wwwroot ainda não existir naquele momento (ex.: primeiro deploy),
// mesmo criando a pasta logo acima.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
});

app.UseCors(WebCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapProjectEndpoints();
app.MapProjectImageEndpoints();

app.Run();

public partial class Program;
