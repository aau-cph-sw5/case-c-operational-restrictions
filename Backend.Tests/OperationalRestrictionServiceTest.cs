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
            new CancelledRefused(),
            new OriginatorCancellationSignatureIsFound(),
            new DutyOperationsManagerCancellationSignatureIsFound(),
            new ControlRoomCancellationSignatureIsFound(),
            new OperatorCancellationSignaturesAreFound()
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

    // Drives a restriction through the approval chain up to Active,
    // returning the active version and the original restriction id.
    private static async Task<(OperationalRestriction Active, Guid OriginalRestrictionId)>
        DriveToActiveAsync(
            OperationalRestrictionService service,
            Guid originator,
            Guid dutyOperationsManager,
            Guid controlRoomSupervisor)
    {
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.RestrictionId, originator);
        var approved = await service.ApproveRestrictionAsync(submitted.RestrictionId, dutyOperationsManager);
        var pendingSupervisor = await service.SubmitForControlRoomSupervisorApprovalAsync(approved.RestrictionId, dutyOperationsManager);
        var active = await service.ActivateRestrictionAsync(pendingSupervisor.RestrictionId, controlRoomSupervisor);

        return (active, draft.RestrictionId);
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
    public async Task RestrictionCanBeCancelledFromActiveToArchived()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var events = new FakeRestrictionTransitionEventRepository();
        var service = CreateService(repo, events);

        var originator = Guid.NewGuid();
        var dutyOperationsManager = Guid.NewGuid();
        var controlRoomSupervisor = Guid.NewGuid();
        var operatorUser = Guid.NewGuid();

        var (active, originalRestrictionId) = await DriveToActiveAsync(
            service, originator, dutyOperationsManager, controlRoomSupervisor);

        // Act
        var pendingCancellation = await service.StartCancellationAsync(active.RestrictionId, originator);
        var outOfForce = await service.ApproveCancellationAsync(pendingCancellation.RestrictionId, dutyOperationsManager);
        var awaitingOperatorSignOut = await service.ControlRoomCancellationAsync(outOfForce.RestrictionId, controlRoomSupervisor);
        var archived = await service.OperatorsArchiveAsync(awaitingOperatorSignOut.RestrictionId, operatorUser);

        // Assert
        Assert.Equal(RestrictionState.PendingCancellation, pendingCancellation.State);
        Assert.Equal(RestrictionState.OutOfForce, outOfForce.State);
        Assert.Equal(RestrictionState.AwaitingOperatorSignOut, awaitingOperatorSignOut.State);
        Assert.Equal(RestrictionState.Archived, archived.State);

        Assert.Equal(operatorUser, archived.CreatedBy);
        Assert.Equal(active.Version + 4, archived.Version);
        Assert.Equal(9, repo.Restrictions.Count);

        // Every cancellation step emits an audit event with actor and states
        var cancellationEvents = events.Events.Where(
            e => e.Action == RestrictionAction.OriginatorStartCancellation
                || e.Action == RestrictionAction.DutyOperationsManagerApproveCancellation
                || e.Action == RestrictionAction.ControlRoomCancellation
                || e.Action == RestrictionAction.OperatorsArchive)
            .ToList();

        Assert.Equal(4, cancellationEvents.Count);
        Assert.All(cancellationEvents, e =>
            Assert.Equal(originalRestrictionId, e.OriginalRestrictionId));

        Assert.Equal(RestrictionState.Active, cancellationEvents[0].FromState);
        Assert.Equal(originator, cancellationEvents[0].ActorId);
        Assert.Equal(RestrictionState.PendingCancellation, cancellationEvents[0].ToState);

        Assert.Equal(RestrictionState.PendingCancellation, cancellationEvents[1].FromState);
        Assert.Equal(dutyOperationsManager, cancellationEvents[1].ActorId);
        Assert.Equal(RestrictionState.OutOfForce, cancellationEvents[1].ToState);

        Assert.Equal(RestrictionState.OutOfForce, cancellationEvents[2].FromState);
        Assert.Equal(controlRoomSupervisor, cancellationEvents[2].ActorId);
        Assert.Equal(RestrictionState.AwaitingOperatorSignOut, cancellationEvents[2].ToState);

        Assert.Equal(RestrictionState.AwaitingOperatorSignOut, cancellationEvents[3].FromState);
        Assert.Equal(operatorUser, cancellationEvents[3].ActorId);
        Assert.Equal(RestrictionState.Archived, cancellationEvents[3].ToState);
    }

    [Fact]
    public async Task CancellationCannotSkipSteps()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);

        var originator = Guid.NewGuid();
        var dutyOperationsManager = Guid.NewGuid();
        var controlRoomSupervisor = Guid.NewGuid();
        var (active, _) = await DriveToActiveAsync(service, originator, dutyOperationsManager, controlRoomSupervisor);
        var pendingCancellation = await service.StartCancellationAsync(active.RestrictionId, originator);

        // Act & Assert - control room cannot cancel before duty operations
        // manager has approved the cancellation
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ControlRoomCancellationAsync(pendingCancellation.RestrictionId, Guid.NewGuid()));
    }

    [Fact]
    public async Task CancellationCannotStartFromNonActiveState()
    {
        // Arrange
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var draft = await CreateDraftAsync(service, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartCancellationAsync(draft.RestrictionId, Guid.NewGuid()));
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
