using Backend.Models.Enum;

namespace Backend.Models.Results;

public record NotificationSendResult(SendOutcome Outcome, string? Error = null)
{
  public static NotificationSendResult Accepted() => new(SendOutcome.Accepted);

  public static NotificationSendResult TransientFailure(string error) => new(SendOutcome.TransientFailure, error);

  public static NotificationSendResult PermanentFailure(string error) => new(SendOutcome.PermanentFailure, error);
}
