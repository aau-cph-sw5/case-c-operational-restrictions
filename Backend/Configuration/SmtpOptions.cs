namespace Backend.Configuration;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public bool EnableSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@metro.local";

    public string FromName { get; set; } = "Operational Restrictions";
}
