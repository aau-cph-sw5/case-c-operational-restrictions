namespace Backend.Models.Enums;

// What happened to a notification. Each event is stored as its own audit entry (C-021).
public enum NotificationAuditEventType
{
    Created,
    Delivered,
    DeliveryFailed,
    Undeliverable,
    Read,
    Resolved
}
