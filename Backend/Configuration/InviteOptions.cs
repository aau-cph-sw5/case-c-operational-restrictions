namespace Backend.Configuration;

public class InviteOptions
{
    public const string SectionName = "Invite";

    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public int TokenLifetimeHours { get; set; } = 48;
}
