namespace Portfolio.Api.Options;

public class DevUserSeedOptions
{
    public const string SectionName = "DevUser";

    public required string Email { get; set; }
    public required string Password { get; set; }
}
