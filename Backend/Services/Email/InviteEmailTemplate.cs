using System.Globalization;

namespace Backend.Services.Email;

public static class InviteEmailTemplate
{
    public const string Subject = "You have been invited to Operational Restrictions";

    public static string Build(string recipientName, string inviteLink, DateTime expiresAtUtc)
    {
        var expires = expiresAtUtc.ToString("dd-MM-yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);

        return $"""
            Welcome, {recipientName}

            You have been invited to the Operational Restrictions system.
            Open the link below to create your password and activate your account:

            {inviteLink}

            The link expires {expires} and can only be used once.

            If you did not expect this invitation, you can ignore this email.
            """;
    }
}
