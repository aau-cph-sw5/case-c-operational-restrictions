namespace Backend.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "backend";

    public string Audience { get; set; } = "backend";

    public int LifetimeDays { get; set; } = 14;
}
