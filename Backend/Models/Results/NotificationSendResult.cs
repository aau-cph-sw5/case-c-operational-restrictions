using Backend.Models.Enum;

namespace Backend.Models.Results;

public record NotificationSendResult(DeliveryStatus Status, string? Error = null)
{
  public static NotificationSendResult Accepted() => new(DeliveryStatus.Accepted);

  public static NotificationSendResult Failed(string error) => new(DeliveryStatus.Failed, error);

  public static NotificationSendResult Undeliverable(string error) => new(DeliveryStatus.Undeliverable, error);
}
