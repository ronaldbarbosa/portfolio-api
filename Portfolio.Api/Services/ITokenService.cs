using Portfolio.Api.Models;

namespace Portfolio.Api.Services;

public interface ITokenService
{
    string CreateToken(ApplicationUser user);
}
