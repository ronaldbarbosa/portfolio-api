using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Portfolio.Api.Dtos;
using Portfolio.Api.Models;
using Portfolio.Api.Options;
using Portfolio.Api.Services;

namespace Portfolio.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");
        group.MapPost("/login", LoginAsync);
        return group;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email e senha são obrigatórios."],
            });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Results.Unauthorized();
        }

        var token = tokenService.CreateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpiryMinutes);

        return Results.Ok(new LoginResponse(token, expiresAt));
    }
}
