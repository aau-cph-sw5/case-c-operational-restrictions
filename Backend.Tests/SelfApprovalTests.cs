using Backend.Exceptions;
using Backend.Lifecycle;
using Backend.Models.Enums;
using Backend.Models.Requests;
using Backend.Services;

namespace Backend.Tests;

// Separation of duties: a user whose role allows approving restrictions must
// still not be able to approve a restriction they created themselves.
public class SelfApprovalTests
{
    [Fact]
    public async Task OriginatorCannotApproveTheirOwnDraft()
    {
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);

        var exception = await Assert.ThrowsAsync<SelfApprovalNotAllowedException>(
            () => service.ApproveRestrictionAsync(draft.Id, originator));

        Assert.Contains("cannot approve", exception.Message);
        // The refused approval must not create a new version.
        Assert.Single(repo.Restrictions);
    }

    [Fact]
    public async Task OriginatorCannotApproveTheirOwnSubmittedRestriction()
    {
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.Id, originator);

        await Assert.ThrowsAsync<SelfApprovalNotAllowedException>(
            () => service.ApproveRestrictionAsync(submitted.Id, originator));

        Assert.Equal(2, repo.Restrictions.Count);
    }

    [Fact]
    public async Task SelfApprovalIsRefusedEvenThoughTheRestrictionIsPendingApproval()
    {
        // The restriction is in the state where approval is normally possible;
        // the originator must still be rejected with the self-approval error
        // rather than a state/guard error.
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var originator = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.Id, originator);
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, submitted.State);

        var exception = await Assert.ThrowsAsync<SelfApprovalNotAllowedException>(
            () => service.ApproveRestrictionAsync(submitted.Id, originator));

        Assert.IsNotType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task AnotherUserWithTheApprovingRoleCanApproveTheSameRestriction()
    {
        var repo = new FakeOperationalRestrictionRepository();
        var service = CreateService(repo);
        var originator = Guid.NewGuid();
        var approver = Guid.NewGuid();
        var draft = await CreateDraftAsync(service, originator);
        var submitted = await service.SubmitForApprovalAsync(draft.Id, originator);

        var approved = await service.ApproveRestrictionAsync(submitted.Id, approver);

        Assert.Equal(RestrictionState.Approved, approved.State);
        Assert.Equal(originator, approved.OriginatorUserId);
    }

    private static OperationalRestrictionService CreateService(
        FakeOperationalRestrictionRepository repository,
        FakeRestrictionTransitionEventRepository? eventRepository = null)
    {
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

        return new OperationalRestrictionService(
            lifecycle,
            repository,
            eventRepository ?? new FakeRestrictionTransitionEventRepository(),
            new FakeUnitOfWork());
    }

    private static Task<Backend.Models.DTOs.OperationalRestrictionDto> CreateDraftAsync(
        OperationalRestrictionService service,
        Guid creator)
    {
        return service.CreateRestrictionAsync(
            new CreateRestrictionRequest(
                CreatedBy: creator,
                Title: "Maintenance work",
                StartDate: DateTime.UtcNow,
                EndDate: DateTime.UtcNow.AddHours(8),
                SituationDescription: "Situation details",
                RestrictionDescription: "Maintenance work",
                CancellationDescription: "Cancellation details"
            ));
    }
}
