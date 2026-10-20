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
  private readonly Dictionary<NotificationChannel, INotificationSender> _senders =
    notificationSenders.ToDictionary(x => x.Channel);
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
    if (!_senders.TryGetValue(delivery.Channel, out var sender))
      return NotificationSendResult.Undeliverable($"No sender is configured for {delivery.Channel}.");

    try
    {
      return await sender.SendAsync(delivery.Notification, cancellationToken);
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
      "Notification delivery {DeliveryId} on {Channel} is undeliverable after {AttemptCount} attempts: {Error}",
      delivery.Id, delivery.Channel, delivery.AttemptCount, delivery.LastError);

    AddEmailFallback(delivery, now);
  }

  private TimeSpan GetRetryDelay(int attemptCount)
  {
    var delaySeconds = _options.BaseRetryDelaySeconds * Math.Pow(2, attemptCount - 1);
    return TimeSpan.FromSeconds(Math.Min(delaySeconds, _options.MaxRetryDelaySeconds));
  }

  // Email is only a fallback for push, and only when an email sender is registered.
  private void AddEmailFallback(NotificationDelivery failedDelivery, DateTime now)
  {
    if (failedDelivery.Channel != NotificationChannel.WebPush || !_senders.ContainsKey(NotificationChannel.Email))
      return;

    _appDbContext.Add(new NotificationDelivery
    {
      Id = Guid.NewGuid(),
      NotificationId = failedDelivery.NotificationId,
      Channel = NotificationChannel.Email,
      Status = DeliveryStatus.Pending,
      AttemptCount = 0,
      NextAttemptAt = now
    });
  }
}
