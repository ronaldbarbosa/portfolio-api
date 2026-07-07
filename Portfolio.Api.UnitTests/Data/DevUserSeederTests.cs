using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Portfolio.Api.Data;
using Portfolio.Api.Models;
using Portfolio.Api.Options;

namespace Portfolio.Api.UnitTests.Data;

public class DevUserSeederTests
{
    private const string DevEmail = "dev@test.local";
    private const string DevPassword = "Password!123";

    private static UserManager<ApplicationUser> CreateUserManagerSubstitute()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        return Substitute.For<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);
    }

    private static IServiceProvider BuildServiceProvider(UserManager<ApplicationUser> userManager)
    {
        var services = new ServiceCollection();
        services.AddSingleton(userManager);
        services.AddSingleton<IOptions<DevUserSeedOptions>>(
            Microsoft.Extensions.Options.Options.Create(new DevUserSeedOptions { Email = DevEmail, Password = DevPassword }));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SeedAsync_SkipsCreation_WhenDevUserAlreadyExists()
    {
        var userManager = CreateUserManagerSubstitute();
        userManager.FindByEmailAsync(DevEmail).Returns(new ApplicationUser { Email = DevEmail });

        await DevUserSeeder.SeedAsync(BuildServiceProvider(userManager));

        await userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SeedAsync_CreatesDevUser_WhenNotExists()
    {
        var userManager = CreateUserManagerSubstitute();
        userManager.FindByEmailAsync(DevEmail).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), DevPassword).Returns(IdentityResult.Success);

        await DevUserSeeder.SeedAsync(BuildServiceProvider(userManager));

        await userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.Email == DevEmail && u.UserName == DevEmail && u.EmailConfirmed),
            DevPassword);
    }

    [Fact]
    public async Task SeedAsync_Throws_WhenCreationFails()
    {
        var userManager = CreateUserManagerSubstitute();
        userManager.FindByEmailAsync(DevEmail).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), DevPassword)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Falha simulada." }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DevUserSeeder.SeedAsync(BuildServiceProvider(userManager)));
    }
}
