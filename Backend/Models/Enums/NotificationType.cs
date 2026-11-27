using System.Text.Json.Serialization;

namespace Backend.Models.Enums;

// Serialized as its name ("SignatureRequired"), not its number, because the
// frontend maps notifications by these string values.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    SignatureRequired,
    ChangesRequested,
    SystemInfo
}
