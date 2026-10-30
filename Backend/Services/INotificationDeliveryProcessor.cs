namespace Backend.Services;

public interface INotificationDeliveryProcessor
{
    Task<int> ProcessDueDeliveriesAsync(CancellationToken cancellationToken);
}
