using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Portfolio.Api.Data;
using Portfolio.Api.Models;
using Portfolio.Api.Options;

namespace Portfolio.Api.UnitTests.Data;

public class AdminSeederTests
{
    private const string AdminEmail = "admin@test.dev";
    private const string AdminPassword = "Password!123";

    private static UserManager<ApplicationUser> CreateUserManagerSubstitute()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        return Substitute.For<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);
    }

    private static IServiceProvider BuildServiceProvider(UserManager<ApplicationUser> userManager)
    {
        var services = new ServiceCollection();
        services.AddSingleton(userManager);
        services.AddSingleton<IOptions<AdminSeedOptions>>(
            Microsoft.Extensions.Options.Options.Create(new AdminSeedOptions { Email = AdminEmail, Password = AdminPassword }));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SeedAsync_SkipsCreation_WhenAdminAlreadyExists()
    {
        var userManager = CreateUserManagerSubstitute();
        userManager.FindByEmailAsync(AdminEmail).Returns(new ApplicationUser { Email = AdminEmail });

        await AdminSeeder.SeedAsync(BuildServiceProvider(userManager));

        await userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SeedAsync_CreatesAdmin_WhenNotExists()
    {
        var userManager = CreateUserManagerSubstitute();
        userManager.FindByEmailAsync(AdminEmail).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), AdminPassword).Returns(IdentityResult.Success);

        await AdminSeeder.SeedAsync(BuildServiceProvider(userManager));

        await userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.Email == AdminEmail && u.UserName == AdminEmail && u.EmailConfirmed),
            AdminPassword);
    }

    [Fact]
    public async Task SeedAsync_Throws_WhenCreationFails()
    {
        var userManager = CreateUserManagerSubstitute();
        userManager.FindByEmailAsync(AdminEmail).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), AdminPassword)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Falha simulada." }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => AdminSeeder.SeedAsync(BuildServiceProvider(userManager)));
    }
}
