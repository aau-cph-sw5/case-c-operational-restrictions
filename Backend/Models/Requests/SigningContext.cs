using Backend.Models.Enums;

namespace Backend.Models.Requests;

// What the server knows about one signing attempt besides who is signing:
// the device class and raw user agent (derived server-side) and the optional
// client-generated key that makes a retry of the same attempt idempotent.
public sealed record SigningContext(DeviceClass DeviceClass, string UserAgent, Guid? AttemptKey)
{
    public static SigningContext Unknown { get; } = new(DeviceClass.Unknown, string.Empty, null);
}
