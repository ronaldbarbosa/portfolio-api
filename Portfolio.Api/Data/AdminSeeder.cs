using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Portfolio.Api.Models;
using Portfolio.Api.Options;

namespace Portfolio.Api.Data;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminOptions = services.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        var existingAdmin = await userManager.FindByEmailAsync(adminOptions.Email);
        if (existingAdmin is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminOptions.Email,
            Email = adminOptions.Email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, adminOptions.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Falha ao criar o usuário admin: {errors}");
        }
    }
}
