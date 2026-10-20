namespace Backend.Models.Enum;

public enum SendOutcome
{
  // The push service or mail server accepted the message.
  Accepted,
  // The send failed but may succeed later, for example a timeout or a 5xx response.
  TransientFailure,
  // Retrying will not help, for example an expired push subscription (404/410).
  PermanentFailure
}
