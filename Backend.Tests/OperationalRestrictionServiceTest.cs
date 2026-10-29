using Backend.Data;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;

namespace Backend.Tests;

public class OperationalRestrictionServiceTests
{
    private static OperationalRestrictionService CreateService(
    FakeOperationalRestrictionRepository repository,
    FakeRestrictionTransitionEventRepository? eventRepository = null)
    {
        var lifecycle = new RestrictionLifecycleService(
            new IRestrictionGuard[]
            {
            new StartDateIsFoundGuard(),
            new OperationalRestictionIdIsFound(),
            new DescriptionTextIsFound(),
            new OriginatorSignatureIsFound(),
            new DutyOperationsManagerSignatureIsFound(),
            new ControlRoomSupervisorSignatureIsFound(),
            new OperatorsAreAssigned(),
            new ReturnedForChanges(),
            new CancelledRefused()
            });

        return new OperationalRestrictionService(
            lifecycle,
            repository,
            eventRepository ?? new FakeRestrictionTransitionEventRepository());
    }

    private static Task<OperationalRestrictionDto> CreateDraftAsync(
        OperationalRestrictionService service,
        Guid creator,
        string description = "Maintenance work")
    {
        return service.CreateRestrictionAsync(
            new CreateRestrictionRequest(
                creator,
                description,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(8)));
    }

    [Fact]
    public async Task EditRestrictionCreatesNewVersionAndLeavesOriginalUntouched()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);

        var creator = Guid.NewGuid();
        var created = await CreateDraftAsync(service, creator);

        // Act
        var editor = Guid.NewGuid();
        await service.EditRestrictionAsync(
            created.RestrictionId,
            new OperationalRestrictionDto
            {
                CreatedBy = editor,
                RestrictionDescription = "Maintenance work (updated)"
            });

        // Assert
        Assert.Equal(2, repo.Restrictions.Count);

        var original = repo.Restrictions.Single(
            r => r.RestrictionId == created.RestrictionId);
        Assert.Null(original.OriginalOperationalRestrictionId);
        Assert.Equal(0, original.Version);
        Assert.Equal("Maintenance work", original.RestrictionDescription);
        Assert.Equal(creator, original.CreatedBy);

        var clone = repo.Restrictions.Single(
            r => r.RestrictionId != created.RestrictionId);
        Assert.Equal(created.RestrictionId, clone.OriginalOperationalRestrictionId);
        Assert.Equal(1, clone.Version);
        Assert.Equal("Maintenance work (updated)", clone.RestrictionDescription);
        Assert.Equal(editor, clone.CreatedBy);

        Assert.False(repo.UpdateWasCalled);
    }

    [Fact]
    public async Task TransitionCreatesNewVersionWithNewStateAndLeavesOldVersionUntouched()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);

        var creator = Guid.NewGuid();
        var created = await CreateDraftAsync(service, creator);

        // Act
        var submitted = await service.SubmitForApprovalAsync(
            created.RestrictionId,
            creator);

        // Assert
        Assert.Equal(2, repo.Restrictions.Count);

        var original = repo.Restrictions.Single(
            r => r.RestrictionId == created.RestrictionId);
        Assert.Equal(RestrictionState.Draft, original.State);
        Assert.Equal(0, original.Version);
        Assert.Null(original.OriginalOperationalRestrictionId);

        Assert.NotEqual(created.RestrictionId, submitted.RestrictionId);
        Assert.Equal(created.RestrictionId, submitted.OriginalOperationalRestrictionId);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, submitted.State);
        Assert.Equal(1, submitted.Version);

        Assert.False(repo.UpdateWasCalled);
    }

    [Fact]
    public async Task RestrictionCanBeDrivenFromDraftToActive()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);

        var originator = Guid.NewGuid();
        var dutyOperationsManager = Guid.NewGuid();
        var controlRoomSupervisor = Guid.NewGuid();

        // Act
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.RestrictionId, originator);
        var returned = await service.ReturnForChangesAsync(submitted.RestrictionId, dutyOperationsManager, "Add track section");
        var resubmittedDraft = await service.ResubmitRestrictionAsync(returned.RestrictionId, originator);
        var submittedAgain = await service.SubmitForApprovalAsync(resubmittedDraft.RestrictionId, originator);
        var approved = await service.ApproveRestrictionAsync(submittedAgain.RestrictionId, dutyOperationsManager);
        var pendingSupervisor = await service.SubmitForControlRoomSupervisorApprovalAsync(approved.RestrictionId, dutyOperationsManager);
        var active = await service.ActivateRestrictionAsync(pendingSupervisor.RestrictionId, controlRoomSupervisor);

        // Assert
        Assert.Equal(RestrictionState.Draft, draft.State);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, submitted.State);
        Assert.Equal(RestrictionState.ReturnedForChanges, returned.State);
        Assert.Equal(RestrictionState.Draft, resubmittedDraft.State);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, submittedAgain.State);
        Assert.Equal(RestrictionState.Approved, approved.State);
        Assert.Equal(RestrictionState.PendingControlRoomSupervisorApproval, pendingSupervisor.State);
        Assert.Equal(RestrictionState.Active, active.State);

        Assert.Equal(7, active.Version);
        Assert.Equal(controlRoomSupervisor, active.CreatedBy);
        Assert.Equal(8, repo.Restrictions.Count);
    }

    [Fact]
    public async Task DenyMovesRestrictionToCancelledRefusedAndStoresReason()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.RestrictionId, originator);

        // Act
        var denied = await service.DenyRestrictionAsync(
            submitted.RestrictionId, Guid.NewGuid(), "Wrong track section");

        // Assert
        Assert.Equal(RestrictionState.CancelledRefused, denied.State);
        Assert.Equal("Wrong track section", denied.ReviewDescription);
    }

    [Fact]
    public async Task ReturnForChangesStoresReviewerAndReason()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var originator = Guid.NewGuid();
        var reviewer = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.RestrictionId, originator);

        // Act
        var returned = await service.ReturnForChangesAsync(
            submitted.RestrictionId, reviewer, "Add track section");

        // Assert
        Assert.Equal(RestrictionState.ReturnedForChanges, returned.State);
        Assert.Equal(reviewer, returned.ReviewerId);
        Assert.Equal("Add track section", returned.ReviewDescription);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReturnForChangesWithoutReasonIsRefused(string reason)
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var events = new FakeRestrictionTransitionEventRepository();
        var service = CreateService(repo, events);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.RestrictionId, originator);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReturnForChangesAsync(submitted.RestrictionId, Guid.NewGuid(), reason));

        // Assert
        Assert.Contains("ReturnedForChanges", exception.Message);
        Assert.Equal(2, repo.Restrictions.Count);
        Assert.Equal(2, events.Events.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DenyWithoutReasonIsRefused(string reason)
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var events = new FakeRestrictionTransitionEventRepository();
        var service = CreateService(repo, events);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.RestrictionId, originator);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DenyRestrictionAsync(submitted.RestrictionId, Guid.NewGuid(), reason));

        // Assert
        Assert.Contains("CancelledRefused", exception.Message);
        Assert.Equal(2, repo.Restrictions.Count);
        Assert.Equal(2, events.Events.Count);
    }

    [Fact]
    public async Task EveryTransitionEmitsAuditEventWithActorAndStates()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var events = new FakeRestrictionTransitionEventRepository();
        var service = CreateService(repo, events);

        var originator = Guid.NewGuid();
        var dutyOperationsManager = Guid.NewGuid();

        // Act
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.RestrictionId, originator);
        var approved = await service.ApproveRestrictionAsync(submitted.RestrictionId, dutyOperationsManager);

        // Assert
        Assert.Equal(3, events.Events.Count);
        Assert.All(events.Events, e => Assert.Equal(draft.RestrictionId, e.OriginalRestrictionId));

        var created = events.Events[0];
        Assert.Equal(RestrictionAction.CreateDraft, created.Action);
        Assert.Null(created.FromState);
        Assert.Equal(RestrictionState.Draft, created.ToState);
        Assert.Equal(originator, created.ActorId);
        Assert.Equal(draft.RestrictionId, created.RestrictionVersionId);

        var submittedEvent = events.Events[1];
        Assert.Equal(RestrictionAction.PendingDutyOperationsManagerApproval, submittedEvent.Action);
        Assert.Equal(RestrictionState.Draft, submittedEvent.FromState);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, submittedEvent.ToState);
        Assert.Equal(originator, submittedEvent.ActorId);
        Assert.Equal(submitted.RestrictionId, submittedEvent.RestrictionVersionId);

        var approvedEvent = events.Events[2];
        Assert.Equal(RestrictionAction.Approved, approvedEvent.Action);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, approvedEvent.FromState);
        Assert.Equal(RestrictionState.Approved, approvedEvent.ToState);
        Assert.Equal(dutyOperationsManager, approvedEvent.ActorId);
        Assert.Equal(approved.RestrictionId, approvedEvent.RestrictionVersionId);
    }

    [Fact]
    public async Task RefusedTransitionNamesGuardAndEmitsNoEvent()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var events = new FakeRestrictionTransitionEventRepository();
        var service = CreateService(repo, events);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator, description: "");

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitForApprovalAsync(draft.RestrictionId, originator));

        // Assert
        Assert.Contains("DescriptionTextIsFound", exception.Message);
        Assert.Single(events.Events);
        Assert.Single(repo.Restrictions);
    }

    [Fact]
    public async Task TransitionFromWrongStateIsRefused()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var draft = await CreateDraftAsync(service, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ActivateRestrictionAsync(draft.RestrictionId, Guid.NewGuid()));
        Assert.Single(repo.Restrictions);
    }

    [Fact]
    public async Task TransitionOfOutdatedVersionIsRefused()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        await service.SubmitForApprovalAsync(draft.RestrictionId, originator);

        // Act - the original draft version is no longer the latest
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitForApprovalAsync(draft.RestrictionId, originator));

        // Assert
        Assert.Contains("outdated", exception.Message);
        Assert.Equal(2, repo.Restrictions.Count);
    }
}
