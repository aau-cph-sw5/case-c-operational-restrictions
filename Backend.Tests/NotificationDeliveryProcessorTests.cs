using Backend.Configuration;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Results;
using Backend.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backend.Tests;

public class NotificationDeliveryProcessorTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly NotificationDeliveryOptions _options = new() { MaxAttempts = 3 };

    public NotificationDeliveryProcessorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notification-delivery-{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AcceptedDeliveryRecordsDeliveredAuditEntry()
    {
        var notification = SeedNotificationWithDueDelivery(attemptCount: 0);
        var processor = CreateProcessor(NotificationSendResult.Accepted());

        await processor.ProcessDueDeliveriesAsync(CancellationToken.None);

        var entry = Assert.Single(_db.NotificationAuditEntries);
        Assert.Equal(NotificationAuditEventType.Delivered, entry.EventType);
        Assert.Equal(notification.Id, entry.NotificationId);
        Assert.Equal(notification.RecipientUserId, entry.RecipientUserId);
        Assert.Equal(notification.OperationalRestrictionId, entry.OperationalRestrictionId);
        Assert.Null(entry.Details);
    }

    [Fact]
    public async Task FailedDeliveryWithAttemptsLeftRecordsDeliveryFailedAuditEntryWithError()
    {
        SeedNotificationWithDueDelivery(attemptCount: 0);
        var processor = CreateProcessor(NotificationSendResult.Failed("Push service unavailable"));

        await processor.ProcessDueDeliveriesAsync(CancellationToken.None);

        var entry = Assert.Single(_db.NotificationAuditEntries);
        Assert.Equal(NotificationAuditEventType.DeliveryFailed, entry.EventType);
        Assert.Equal("Push service unavailable", entry.Details);
    }

    [Fact]
    public async Task FailedDeliveryOnLastAttemptRecordsUndeliverableAuditEntry()
    {
        SeedNotificationWithDueDelivery(attemptCount: _options.MaxAttempts - 1);
        var processor = CreateProcessor(NotificationSendResult.Failed("Push service unavailable"));

        await processor.ProcessDueDeliveriesAsync(CancellationToken.None);

        var entry = Assert.Single(_db.NotificationAuditEntries);
        Assert.Equal(NotificationAuditEventType.Undeliverable, entry.EventType);
        Assert.Equal("Push service unavailable", entry.Details);
    }

    [Fact]
    public async Task DeliveryWithoutSenderRecordsUndeliverableAuditEntry()
    {
        SeedNotificationWithDueDelivery(attemptCount: 0);
        var processor = CreateProcessor(sendResult: null);

        await processor.ProcessDueDeliveriesAsync(CancellationToken.None);

        var entry = Assert.Single(_db.NotificationAuditEntries);
        Assert.Equal(NotificationAuditEventType.Undeliverable, entry.EventType);
    }

    [Fact]
    public async Task EachDeliveryAttemptAddsANewAuditEntry()
    {
        SeedNotificationWithDueDelivery(attemptCount: 0);
        var processor = CreateProcessor(NotificationSendResult.Failed("Push service unavailable"));

        await processor.ProcessDueDeliveriesAsync(CancellationToken.None);
        var delivery = _db.NotificationDeliveries.Single();
        delivery.NextAttemptAt = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();
        await processor.ProcessDueDeliveriesAsync(CancellationToken.None);

        var entries = _db.NotificationAuditEntries.ToList();
        Assert.Equal(2, entries.Count);
        Assert.All(entries, x => Assert.Equal(NotificationAuditEventType.DeliveryFailed, x.EventType));
    }

    // A null sendResult means no sender is registered.
    private NotificationDeliveryProcessor CreateProcessor(NotificationSendResult? sendResult)
    {
        var senders = sendResult == null
            ? []
            : new List<INotificationSender> { new FakeNotificationSender(sendResult) };

        return new NotificationDeliveryProcessor(
            _db,
            senders,
            Options.Create(_options),
            TimeProvider.System,
            NullLogger<NotificationDeliveryProcessor>.Instance);
    }

    private Notification SeedNotificationWithDueDelivery(int attemptCount)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = Guid.NewGuid(),
            OperationalRestrictionId = Guid.NewGuid(),
            Type = NotificationType.SignatureRequired,
            RequiresAction = true,
            CreatedAt = DateTime.UtcNow,
            Deliveries =
            [
                new NotificationDelivery
                {
                    Id = Guid.NewGuid(),
                    Status = attemptCount == 0 ? DeliveryStatus.Pending : DeliveryStatus.Failed,
                    AttemptCount = attemptCount,
                    NextAttemptAt = DateTime.UtcNow.AddMinutes(-1)
                }
            ]
        };

        _db.Notifications.Add(notification);
        _db.SaveChanges();

        return notification;
    }

    private sealed class FakeNotificationSender(NotificationSendResult result) : INotificationSender
    {
        public Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
