using Backend.Configuration;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Models.Enum;
using Backend.Models.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services;

// Sends deliveries that are due and records the result on each delivery row (ADR 0008).
// Assumes a single backend instance; several instances would need row locking.
public class NotificationDeliveryProcessor(
  AppDbContext appDbContext,
  IEnumerable<INotificationSender> notificationSenders,
  IOptions<NotificationDeliveryOptions> options,
  TimeProvider timeProvider,
  ILogger<NotificationDeliveryProcessor> logger
) : INotificationDeliveryProcessor
{
  private readonly AppDbContext _appDbContext = appDbContext;
  private readonly INotificationSender? _sender = notificationSenders.SingleOrDefault();
  private readonly NotificationDeliveryOptions _options = options.Value;
  private readonly TimeProvider _timeProvider = timeProvider;
  private readonly ILogger<NotificationDeliveryProcessor> _logger = logger;

  /// <summary>
  /// Sends every Pending or Failed delivery whose next attempt is due, and saves the results.
  /// Returns the number of deliveries handled.
  /// </summary>
  public async Task<int> ProcessDueDeliveriesAsync(CancellationToken cancellationToken)
  {
    var now = _timeProvider.GetUtcNow().UtcDateTime;

    var dueDeliveries = await _appDbContext.NotificationDeliveries
      .Include(x => x.Notification)
      .Where(x => (x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Failed)
        && x.NextAttemptAt <= now)
      .OrderBy(x => x.NextAttemptAt)
      .Take(_options.BatchSize)
      .ToListAsync(cancellationToken);

    foreach (var delivery in dueDeliveries)
    {
      var result = await SendAsync(delivery, cancellationToken);
      ApplyResult(delivery, result, now);
    }

    await _appDbContext.SaveChangesAsync(cancellationToken);

    return dueDeliveries.Count;
  }

  private async Task<NotificationSendResult> SendAsync(NotificationDelivery delivery, CancellationToken cancellationToken)
  {
    if (_sender == null)
      return NotificationSendResult.Undeliverable("No notification sender is configured.");

    try
    {
      return await _sender.SendAsync(delivery.Notification, cancellationToken);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      // An unexpected error is retried rather than lost.
      _logger.LogWarning(exception, "Sending notification delivery {DeliveryId} threw", delivery.Id);
      return NotificationSendResult.Failed(exception.Message);
    }
  }

  private void ApplyResult(NotificationDelivery delivery, NotificationSendResult result, DateTime now)
  {
    delivery.AttemptCount++;

    if (result.Status == DeliveryStatus.Accepted)
    {
      delivery.Status = DeliveryStatus.Sent;
      delivery.SentAt = now;
      delivery.NextAttemptAt = null;
      delivery.LastError = null;
      return;
    }

    delivery.LastError = result.Error;

    if (result.Status== DeliveryStatus.Failed && delivery.AttemptCount < _options.MaxAttempts)
    {
      delivery.Status = DeliveryStatus.Failed;
      delivery.NextAttemptAt = now + GetRetryDelay(delivery.AttemptCount);
      return;
    }

    delivery.Status = DeliveryStatus.Undeliverable;
    delivery.NextAttemptAt = null;
    _logger.LogWarning(
      "Notification delivery {DeliveryId} is undeliverable after {AttemptCount} attempts: {Error}",
      delivery.Id, delivery.AttemptCount, delivery.LastError);
  }

  private TimeSpan GetRetryDelay(int attemptCount)
  {
    var delaySeconds = _options.BaseRetryDelaySeconds * Math.Pow(2, attemptCount - 1);
    return TimeSpan.FromSeconds(Math.Min(delaySeconds, _options.MaxRetryDelaySeconds));
  }
}
