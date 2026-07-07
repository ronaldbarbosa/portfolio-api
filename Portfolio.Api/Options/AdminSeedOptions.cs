namespace Portfolio.Api.Options;

public class AdminSeedOptions
{
    public const string SectionName = "AdminUser";

    public required string Email { get; set; }
    public required string Password { get; set; }
}
