using Backend.Data;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class NotificationServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notifications-{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetNotificationsForUserAsyncReturnsOnlyTheRecipientsNotifications()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        SeedNotification(Guid.NewGuid(), other, DateTime.UtcNow);
        var expected = SeedNotification(Guid.NewGuid(), user, DateTime.UtcNow);

        var service = new NotificationService(_db);

        var result = await service.GetNotificationsForUserAsync(user);

        Assert.Single(result);
        Assert.Equal(expected, result[0].Id);
        Assert.Equal(user, result[0].RecipientUserId);
    }

    [Fact]
    public async Task GetNotificationsForUserAsyncReturnsNewestFirst()
    {
        var user = Guid.NewGuid();
        var oldest = SeedNotification(Guid.NewGuid(), user, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newest = SeedNotification(Guid.NewGuid(), user, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        var service = new NotificationService(_db);

        var result = await service.GetNotificationsForUserAsync(user);

        Assert.Equal(2, result.Count);
        Assert.Equal(newest, result[0].Id);
        Assert.Equal(oldest, result[1].Id);
    }

    [Fact]
    public async Task GetNotificationsForUserAsyncReturnsEmptyListForUnknownUser()
    {
        var service = new NotificationService(_db);

        var result = await service.GetNotificationsForUserAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddNotificationRecordsCreatedAuditEntry()
    {
        var recipient = Guid.NewGuid();
        var restrictionId = Guid.NewGuid();
        var service = new NotificationService(_db);

        var notificationId = service.AddNotification(
            new CreateNotificationRequest(recipient, NotificationType.SignatureRequired, true, restrictionId));
        await _db.SaveChangesAsync();

        var entry = Assert.Single(_db.NotificationAuditEntries);
        Assert.Equal(notificationId, entry.NotificationId);
        Assert.Equal(recipient, entry.RecipientUserId);
        Assert.Equal(restrictionId, entry.OperationalRestrictionId);
        Assert.Equal(NotificationAuditEventType.Created, entry.EventType);
    }

    [Fact]
    public async Task ResolveActionRequiredAsyncRecordsResolvedAuditEntry()
    {
        var recipient = Guid.NewGuid();
        var restrictionId = Guid.NewGuid();
        var service = new NotificationService(_db);
        var notificationId = service.AddNotification(
            new CreateNotificationRequest(recipient, NotificationType.SignatureRequired, true, restrictionId));
        await _db.SaveChangesAsync();

        await service.ResolveActionRequiredAsync(restrictionId, recipient);
        await _db.SaveChangesAsync();

        var entry = Assert.Single(_db.NotificationAuditEntries, x => x.EventType == NotificationAuditEventType.Resolved);
        Assert.Equal(notificationId, entry.NotificationId);
        Assert.Equal(restrictionId, entry.OperationalRestrictionId);
    }

    [Fact]
    public async Task NotificationAuditEntriesCannotBeDeleted()
    {
        var service = new NotificationService(_db);
        service.AddNotification(
            new CreateNotificationRequest(Guid.NewGuid(), NotificationType.SignatureRequired, true, Guid.NewGuid()));
        await _db.SaveChangesAsync();

        _db.NotificationAuditEntries.Remove(_db.NotificationAuditEntries.Single());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _db.SaveChangesAsync());
    }

    private Guid SeedNotification(Guid id, Guid recipientUserId, DateTime createdAt)
    {
        _db.Notifications.Add(new Notification
        {
            Id = id,
            RecipientUserId = recipientUserId,
            Type = NotificationType.SystemInfo,
            Title = "Test",
            Message = "Test",
            RequiresAction = false,
            CreatedAt = createdAt
        });

        _db.SaveChanges();

        return id;
    }
}
