namespace Backend.Models.Enum;

public enum DeliveryStatus
{
  Pending,
  Sent,
  // Failed deliveries are retried at NextAttemptAt.
  Failed,
  // Attempts are exhausted or the error is permanent; shown on the restriction so it is not lost.
  Undeliverable
}
