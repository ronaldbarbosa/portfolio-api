using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Portfolio.Api.Models;
using Portfolio.Api.Options;
using Portfolio.Api.Services;

namespace Portfolio.Api.UnitTests.Services;

public class TokenServiceTests
{
    private static TokenService CreateSut(int expiryMinutes = 60) =>
        new(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Key = "unit-test-signing-key-with-enough-length-1234567890",
            Issuer = "Portfolio.Api.Tests",
            Audience = "Portfolio.Api.Tests",
            ExpiryMinutes = expiryMinutes,
        }));

    private static ApplicationUser CreateUser() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Email = "user@example.com",
        UserName = "user@example.com",
    };

    [Fact]
    public void CreateToken_IncludesUserClaimsAndIssuerAudience()
    {
        var sut = CreateSut();
        var user = CreateUser();

        var token = sut.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id, jwt.Subject);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Portfolio.Api.Tests", jwt.Issuer);
        Assert.Contains("Portfolio.Api.Tests", jwt.Audiences);
    }

    [Fact]
    public void CreateToken_SetsExpirationAccordingToOptions()
    {
        var sut = CreateSut(expiryMinutes: 30);
        var user = CreateUser();

        var token = sut.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 10);
    }

    [Fact]
    public void CreateToken_GeneratesUniqueTokenIdPerCall()
    {
        var sut = CreateSut();
        var user = CreateUser();

        var firstToken = new JwtSecurityTokenHandler().ReadJwtToken(sut.CreateToken(user));
        var secondToken = new JwtSecurityTokenHandler().ReadJwtToken(sut.CreateToken(user));

        var firstJti = firstToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = secondToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(firstJti, secondJti);
    }
}
