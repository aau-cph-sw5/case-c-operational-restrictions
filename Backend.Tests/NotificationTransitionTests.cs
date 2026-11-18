using System.Text.Json;

using Backend.Authorization;
using Backend.Data;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

// MET-C-019: reaching a lifecycle step notifies the holders of the role that step waits on,
// and completing the step resolves the notifications so no further reminders are sent.
[Collection("SignatureService")]
public class NotificationTransitionTests : IDisposable
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly OperationalRestrictionService _restrictionService;

    public NotificationTransitionTests()
    {
        _db = TestDatabase.CreateSeededContext();
        var signatureService = new SignatureService(_db);

        var lifecycle = new RestrictionLifecycleService(
            new IRestrictionGuard[]
            {
                new StartDateIsFoundGuard(),
                new OperationalRestrictionIdIsFound(),
                new DescriptionTextIsFound(),
                new OriginatorSignatureIsFound(),
                new DutyOperationsManagerSignatureIsFound(),
                new ControlRoomSupervisorSignatureIsFound(),
                new OperatorsAreAssigned(),
                new ReturnedForChanges(),
                new CancelledRefused()
            });

        _restrictionService = new OperationalRestrictionService(
            lifecycle,
            new OperationalRestrictionRepository(_db),
            new RestrictionTransitionEventRepository(_db),
            new UnitOfWork(_db),
            signatureService,
            new NotificationService(_db, signatureService));
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SubmittingForApprovalNotifiesEveryDutyOperationsManager()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);

        var pending = await _restrictionService.SubmitForApprovalAsync(
            restriction.RestrictionId, restriction.OriginatorUserId);

        var notifications = await OpenNotificationsAsync();
        var managers = await RoleHoldersAsync(RoleNames.DutyOperationsManager);

        Assert.NotEmpty(managers);
        Assert.Equal(managers.OrderBy(x => x), notifications.Select(n => n.RecipientUserId).OrderBy(x => x));
        Assert.All(notifications, n =>
        {
            Assert.Equal(NotificationType.SignatureRequired, n.Type);
            Assert.True(n.RequiresAction);
            Assert.Equal(pending.Id, n.OperationalRestrictionId);
            Assert.Contains(restriction.Title, n.Message);
        });
    }

    [Fact]
    public async Task ReachingControlRoomSupervisorStepNotifiesEveryControlRoomSupervisor()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Approved);

        await _restrictionService.SubmitForControlRoomSupervisorApprovalAsync(
            restriction.RestrictionId, Guid.NewGuid());

        var notifications = await OpenNotificationsAsync();
        var supervisors = await RoleHoldersAsync(RoleNames.ControlroomSupervisor);

        Assert.NotEmpty(supervisors);
        Assert.Equal(supervisors.OrderBy(x => x), notifications.Select(n => n.RecipientUserId).OrderBy(x => x));
        Assert.All(notifications, n => Assert.Equal(NotificationType.SignatureRequired, n.Type));
    }

    [Fact]
    public async Task ReturningForChangesNotifiesTheOriginatorWithTheReason()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.PendingDutyOperationsManagerApproval);

        await _restrictionService.ReturnForChangesAsync(
            restriction.RestrictionId, Guid.NewGuid(), "Mangler slutdato");

        var notification = Assert.Single(await OpenNotificationsAsync());
        Assert.Equal(restriction.OriginatorUserId, notification.RecipientUserId);
        Assert.Equal(NotificationType.ChangesRequested, notification.Type);
        Assert.Contains("Mangler slutdato", notification.Message);
    }

    [Fact]
    public async Task CompletingTheStepResolvesItsNotificationsAndCancelsPendingPushes()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);
        var pending = await _restrictionService.SubmitForApprovalAsync(
            restriction.RestrictionId, restriction.OriginatorUserId);
        var approver = (await RoleHoldersAsync(RoleNames.DutyOperationsManager)).First();

        await _restrictionService.ApproveRestrictionAsync(pending.Id, approver);

        var stepNotifications = await _db.Notifications
            .Include(n => n.Deliveries)
            .Where(n => n.Type == NotificationType.SignatureRequired)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(stepNotifications);
        Assert.All(stepNotifications, n =>
        {
            Assert.NotNull(n.ResolvedAt);
            Assert.All(n.Deliveries, d => Assert.Equal(DeliveryStatus.Cancelled, d.Status));
        });

        var resolvedAudits = await _db.NotificationAuditEntries
            .CountAsync(a => a.EventType == NotificationAuditEventType.Resolved, TestContext.Current.CancellationToken);
        Assert.Equal(stepNotifications.Count, resolvedAudits);
    }

    [Fact]
    public async Task TransitionNotificationsAreRecordedInTheAuditTrail()
    {
        var restriction = await AddRestrictionAsync(RestrictionState.Draft);

        await _restrictionService.SubmitForApprovalAsync(
            restriction.RestrictionId, restriction.OriginatorUserId);

        var notificationIds = await _db.Notifications.Select(n => n.Id).ToListAsync(TestContext.Current.CancellationToken);
        var createdAudits = await _db.NotificationAuditEntries
            .Where(a => a.EventType == NotificationAuditEventType.Created)
            .Select(a => a.NotificationId)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(notificationIds);
        Assert.Equal(notificationIds.OrderBy(x => x), createdAudits.OrderBy(x => x));
    }

    [Fact]
    public void NotificationTypeIsSerializedAsItsName()
    {
        var dto = new NotificationDto { Type = NotificationType.SignatureRequired };

        var json = JsonSerializer.Serialize(dto, WebJsonOptions);

        Assert.Contains("\"type\":\"SignatureRequired\"", json);
    }

    private Task<List<Notification>> OpenNotificationsAsync() =>
        _db.Notifications
            .Where(n => n.ResolvedAt == null)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<List<Guid>> RoleHoldersAsync(string roleName) =>
        _db.UserRoleAssignments
            .Where(a => a.Role.Name == roleName)
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(TestContext.Current.CancellationToken);

    private async Task<OperationalRestriction> AddRestrictionAsync(RestrictionState state)
    {
        var originator = Guid.NewGuid();
        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = originator,
            CreatedBy = originator,
            State = state,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            Title = "Sporarbejde Valby",
            SituationDescription = "Test situation",
            RestrictionDescription = "Test restriction",
            CancellationDescription = "Test cancellation"
        };

        _db.OperationalRestrictions.Add(restriction);
        await _db.SaveChangesAsync();
        return restriction;
    }
}
