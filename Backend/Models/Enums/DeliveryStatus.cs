namespace Backend.Models.Enums;

public enum DeliveryStatus
{
    Pending,
    Sent,
    Accepted,
    // Failed deliveries are retried at NextAttemptAt.
    Failed,
    // Attempts are exhausted or the error is permanent; shown on the restriction so it is not lost.
    Undeliverable
    Undeliverable,
    // The notification was resolved before it was sent, so the push is no longer needed.
    Cancelled
}
