using Backend.Models.Enums;

namespace Backend.Signing;

// Derives the device class of a request on the server, so the value recorded
// with a signature is not something the client simply declares.
// Limits: iPadOS Safari in desktop mode sends a Macintosh user agent and is
// reported as Desktop; a remote desktop session is reported as Desktop.
public static class DeviceClassResolver
{
    public const int MaxUserAgentLength = 512;

    private static readonly string[] TabletTokens = ["iPad", "Tablet", "Kindle", "Silk", "PlayBook"];
    private static readonly string[] PhoneTokens = ["iPhone", "iPod", "Windows Phone"];
    private static readonly string[] DesktopTokens = ["Windows NT", "Macintosh", "X11", "CrOS", "Linux"];

    public static DeviceClass Resolve(string? userAgent, string? mobileClientHint)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return DeviceClass.Unknown;

        if (ContainsAny(userAgent, TabletTokens))
            return DeviceClass.Tablet;

        // Android phones say "Mobile" in the user agent, Android tablets do not.
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
                ? DeviceClass.Phone
                : DeviceClass.Tablet;

        if (ContainsAny(userAgent, PhoneTokens) || mobileClientHint?.Trim() == "?1")
            return DeviceClass.Phone;

        return ContainsAny(userAgent, DesktopTokens) ? DeviceClass.Desktop : DeviceClass.Unknown;
    }

    // The raw string is kept so an auditor can re-check the coarse class.
    public static string NormalizeUserAgent(string? userAgent)
    {
        var trimmed = userAgent?.Trim() ?? string.Empty;
        return trimmed.Length <= MaxUserAgentLength ? trimmed : trimmed[..MaxUserAgentLength];
    }

    private static bool ContainsAny(string value, string[] tokens) =>
        tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
}
