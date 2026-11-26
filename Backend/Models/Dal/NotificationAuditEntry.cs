using Backend.Models.Enums;

namespace Backend.Models.Entities;

// Append-only audit entry for a notification (C-021): one row per event, never changed or deleted.
public sealed class NotificationAuditEntry
{
    public required Guid Id { get; init; }
    public required Guid NotificationId { get; init; }
    public required Guid RecipientUserId { get; init; }
    // The exact restriction version the notification was about; null for SystemInfo notifications.
    public Guid? OperationalRestrictionId { get; init; }
    public required NotificationAuditEventType EventType { get; init; }
    public required DateTime OccurredAt { get; init; }
    // Extra information, for example the error of a failed delivery.
    public string? Details { get; init; }
}
