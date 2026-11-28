
using System.Reflection;
using System.Runtime.CompilerServices;

using Backend.Authorization;
using Backend.Data;
using Backend.Exceptions;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;

namespace Backend.Tests;

[Collection("SignatureService")]
public class SignatureServiceTests
{
    // Each test gets its own seeded database and notification service.
    private readonly AppDbContext _appDbContext;
    private readonly FakeNotificationService _notificationService;
    private readonly SignatureService _service;

    public SignatureServiceTests()
    {
        _appDbContext = TestDatabase.CreateSeededContext();
        _notificationService = new FakeNotificationService();
        _service = new SignatureService(_appDbContext, _notificationService);
    }

    [Fact]
    public void SignatureHasNoPublicSetters()
    {
        var mutableProperties = typeof(Signature)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { IsPublic: true }
                && !property.SetMethod.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(typeof(IsExternalInit)))
            .Select(property => property.Name)
            .ToList();

        Assert.Empty(mutableProperties);
    }

    [Fact]
    public void SignRecordsSignerRoleVersionAndServerTimestamp()
    {
        var restrictionVersionId = Guid.NewGuid();
        var required = _service.CreateRequiredSignatures(restrictionVersionId).First();
        var before = DateTime.UtcNow;

        var signature = _service.Sign(required.Id, required.SigneeId);

        Assert.Equal(required.SigneeId, signature.SigneeId);
        Assert.Equal(restrictionVersionId, signature.OperationalRestrictionId);

        var expectedRoles = _appDbContext.UserRoleAssignments
            .Where(a => a.UserId == required.SigneeId)
            .Join(_appDbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.Equal(string.Join(", ", expectedRoles), signature.SigneeRole);
        Assert.True(signature.SignedAt >= before);
    }

    [Fact]
    public void SignOriginatorCreatesImmutableSignatureForRestriction()
    {
        var restrictionId = Guid.NewGuid();
        var originatorId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var signature = _service.SignOriginator(restrictionId, originatorId);

        Assert.Equal(originatorId, signature.SigneeId);
        Assert.Equal(RoleNames.Originator, signature.SigneeRole);
        Assert.Equal(restrictionId, signature.OperationalRestrictionId);
        Assert.True(signature.SignedAt >= before);
        Assert.True(_service.IsSignatureValid(signature.Id));
        Assert.Same(signature, _service.GetOriginatorSignature(restrictionId));
    }

    [Fact]
    public void GetOriginatorSignatureReturnsNullBeforeSigning()
    {
        Assert.Null(_service.GetOriginatorSignature(Guid.NewGuid()));
    }

    [Fact]
    public void SignOriginatorTwiceThrows()
    {
        var restrictionId = Guid.NewGuid();
        var originatorId = Guid.NewGuid();
        _service.SignOriginator(restrictionId, originatorId);

        Assert.Throws<RequiresSignatureNotSignableException>(
            () => _service.SignOriginator(restrictionId, originatorId));
    }

    [Fact]
    public void SignByAnotherUserThrows()
    {
        var required = _service.CreateRequiredSignatures(Guid.NewGuid()).First();

        Assert.Throws<NotRequiredSignerException>(
            () => _service.Sign(required.Id, Guid.NewGuid()));
    }

    [Fact]
    public void SignTwiceThrows()
    {
        var required = _service.CreateRequiredSignatures(Guid.NewGuid()).First();
        _service.Sign(required.Id, required.SigneeId);

        Assert.Throws<RequiresSignatureNotSignableException>(
            () => _service.Sign(required.Id, required.SigneeId));
    }

    [Fact]
    public void InvalidateRequiredSignaturesInvalidatesSignedAndBlocksFurtherSigning()
    {
        var restrictionVersionId = Guid.NewGuid();
        var requiredSignatures = _service.CreateRequiredSignatures(restrictionVersionId);
        var signature = _service.Sign(
            requiredSignatures[0].Id,
            requiredSignatures[0].SigneeId);

        Assert.True(_service.IsSignatureValid(signature.Id));

        _service.InvalidateRequiredSignatures(restrictionVersionId);

        Assert.False(_service.IsSignatureValid(signature.Id));

        Assert.Throws<RequiresSignatureNotSignableException>(() =>
            _service.Sign(requiredSignatures[1].Id, requiredSignatures[1].SigneeId));
    }

    [Fact]
    public void CreateRequiredSignaturesSkipsUsersOnHolidayAndStartsUnsigned()
    {
        var usersNotOnHoliday = _appDbContext.Users
            .Count(x => x.WorkingStatus != UserWorkingStatus.OnHoliday);

        var required = _service.CreateRequiredSignatures(Guid.NewGuid());

        Assert.Equal(usersNotOnHoliday, required.Count);
        Assert.DoesNotContain(required, x => x.SigneeId == DbSeeder.MikkelId);

        Assert.All(required, x =>
            Assert.Equal(RequiresSignatureStatus.PendingSignature, x.RequiresSignatureStatus));
    }

    [Fact]
    public void CreateRequiredSignaturesCreatesNotificationsForEachSigner()
    {
        var notificationService = new FakeNotificationService();
        var dbContext = TestDatabase.CreateSeededContext();
        var service = new SignatureService(dbContext, notificationService);
        var restrictionId = Guid.NewGuid();

        var requiredSignatures = service.CreateRequiredSignatures(restrictionId);

        Assert.Equal(requiredSignatures.Count, notificationService.Notifications.Count);

        foreach (var requiredSignature in requiredSignatures)
        {
            Assert.Contains(notificationService.Notifications, notification =>
                notification.RecipientUserId == requiredSignature.SigneeId &&
                notification.Type == NotificationType.SignatureRequired &&
                notification.RequiresAction &&
                notification.OperationalRestrictionId == restrictionId
            );
        }
    }
}

public class FakeNotificationService : INotificationService
{
    public List<CreateNotificationRequest> Notifications { get; } = [];

    public Guid AddNotification(CreateNotificationRequest request)
    {
        Notifications.Add(request);
        return Guid.NewGuid();
    }

    public Task ResolveActionRequiredAsync(
        Guid operationalRestrictionId,
        Guid recipientUserId)
    {
        return Task.CompletedTask;
    }

    public Task<List<Notification>> GetNotificationsForUserAsync(
        Guid recipientUserId)
    {
        return Task.FromResult(new List<Notification>());
    }

    [Fact]
    public void GetRequiredSignaturesByRestrictionIdReturnsOnlyMatchingSignatures()
    {
        var restrictionId = Guid.NewGuid();
        var otherRestrictionId = Guid.NewGuid();

        var expected = _service.CreateRequiredSignatures(restrictionId);
        _service.CreateRequiredSignatures(otherRestrictionId);

        var result = _service.GetRequiredSignaturesByRestrictionId(restrictionId);

        Assert.Equal(expected.Count, result.Count);
        Assert.All(result, signature =>
            Assert.Equal(restrictionId, signature.OperationalRestrictionId));
    }

    [Fact]
    public void GetSignatureStatusesReturnsCorrectSignerInformationAndStatus()
    {
        var restrictionId = Guid.NewGuid();
        var required = _service.CreateRequiredSignatures(restrictionId);
        var signer = required.First();

        _service.Sign(signer.Id, signer.SigneeId);

        var result = _service.GetSignatureStatuses(restrictionId);

        var signedStatus = Assert.Single(result,
            status => status.RequiresSignatureId == signer.Id);

        var user = _appDbContext.Users.Single(u => u.Id == signer.SigneeId);

        var expectedRoles = _appDbContext.UserRoleAssignments
            .Where(a => a.UserId == signer.SigneeId)
            .Join(_appDbContext.Roles,
                a => a.RoleId,
                r => r.Id,
                (a, r) => r.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.Equal(signer.SigneeId, signedStatus.SigneeId);
        Assert.Equal(user.Name, signedStatus.SigneeName);
        Assert.Equal(string.Join(", ", expectedRoles), signedStatus.SigneeRole);
        Assert.Equal("CompletedSignature", signedStatus.Status);

        Assert.Contains(result, status => status.Status == "PendingSignature");
    }

    [Fact]
    public void GetSignatureStatusesReturnsEmptyWhenNoSignaturesExist()
    {
        var result = _service.GetSignatureStatuses(Guid.NewGuid());

        Assert.Empty(result);
    }
}
