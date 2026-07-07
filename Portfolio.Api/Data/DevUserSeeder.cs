using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Portfolio.Api.Models;
using Portfolio.Api.Options;

namespace Portfolio.Api.Data;

public static class DevUserSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var devOptions = services.GetRequiredService<IOptions<DevUserSeedOptions>>().Value;

        var existingUser = await userManager.FindByEmailAsync(devOptions.Email);
        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = devOptions.Email,
            Email = devOptions.Email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, devOptions.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Falha ao criar o usuário de desenvolvimento: {errors}");
        }
    }
}
