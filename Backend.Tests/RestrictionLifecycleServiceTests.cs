
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Enums;
using Backend.Models.Requests;

namespace Backend.Tests;

public class RestrictionLifecycleServiceTests
{
    private readonly RestrictionLifecycleService _sut;

    public RestrictionLifecycleServiceTests()
    {
        _sut = new RestrictionLifecycleService(
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
    }

    // Guards a transition declares must exist as implementations, so the
    // declaration in RestrictionLifecycle stays the single source of truth.
    [Fact]
    public void EveryDeclaredGuardHasAnImplementation()
    {
        var implementedGuardNames = typeof(IRestrictionGuard).Assembly
            .GetTypes()
            .Where(t => typeof(IRestrictionGuard).IsAssignableFrom(t)
                && t is { IsClass: true, IsAbstract: false })
            .Select(t => ((IRestrictionGuard)Activator.CreateInstance(t)!).Name)
            .ToHashSet();

        var declaredGuardNames = RestrictionLifecycle.Transitions.Values
            .SelectMany(t => t.Guards)
            .Distinct();

        Assert.All(declaredGuardNames, name => Assert.Contains(name, implementedGuardNames));
    }

    [Fact]
    public void EnsureCanTransitionReturnsDeclaredTargetState()
    {
        foreach (var transition in RestrictionLifecycle.Transitions.Values
            .Where(t => t.From is not null))
        {
            var nextState = _sut.EnsureCanTransition(
                transition.From!.Value,
                transition.Action,
                CreateValidDto(transition.From.Value));

            Assert.Equal(transition.To, nextState);
        }
    }

    [Fact]
    public void EnsureCanCreateWithValidDataDoesNotThrow()
    {
        // Arrange
        var data = new CreateRestrictionRequest
        (
            Guid.NewGuid(),
            "Maintenance work",
            DateTime.Now,
            DateTime.Now
        );

        // Act & Assert
        _sut.EnsureCanCreate(data);
    }

    [Fact]
    public void EnsureCanCreateWithInvalidDataIdentifiesFailedGuard()
    {
        // Arrange - StartDate missing (default)
        var data = new CreateRestrictionRequest
        (
            Guid.NewGuid(),
            "Maintenance work",
            default,
            DateTime.Now
        );

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => _sut.EnsureCanCreate(data));

        // Assert - should fail on StartDateIsFound
        Assert.Contains("StartDateIsFound", exception.Message);
    }

    // Helper method to create a valid DTO for testing
    private static OperationalRestrictionDto CreateValidDto(
        RestrictionState state = RestrictionState.Draft)
    {
        return new OperationalRestrictionDto
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = Guid.NewGuid(),
            RestrictionDescription = "Test restriction",
            StartDate = DateTime.Now,
            State = state,
            ReviewerId = Guid.NewGuid(),
            ReviewDescription = "Review comment"
        };
    }

    [Theory]
    [InlineData(RestrictionAction.ReturnedForChanges, "ReturnedForChanges")]
    [InlineData(RestrictionAction.CancelledRefused, "CancelledRefused")]
    public void ReviewTransitionWithoutReasonIsRefused(
        RestrictionAction action, string expectedGuard)
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingDutyOperationsManagerApproval);
        data.ReviewDescription = "  ";

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => _sut.EnsureCanTransition(
                RestrictionState.PendingDutyOperationsManagerApproval, action, data));

        // Assert
        Assert.Contains(expectedGuard, exception.Message);
    }

    [Theory]
    [InlineData(RestrictionAction.ReturnedForChanges, "ReturnedForChanges")]
    [InlineData(RestrictionAction.CancelledRefused, "CancelledRefused")]
    public void ReviewTransitionWithoutReviewerIsRefused(
        RestrictionAction action, string expectedGuard)
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingDutyOperationsManagerApproval);
        data.ReviewerId = Guid.Empty;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => _sut.EnsureCanTransition(
                RestrictionState.PendingDutyOperationsManagerApproval, action, data));

        // Assert
        Assert.Contains(expectedGuard, exception.Message);
    }

    // ========== Draft State Transitions ==========

    [Fact]
    public void EnsureCanTransitionFromDraftToPendingDutyOperationsManagerApproval()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.Draft);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.Draft,
            RestrictionAction.PendingDutyOperationsManagerApproval,
            data);
    }

    // ========== PendingDutyOperationsManagerApproval State Transitions ==========

    [Fact]
    public void EnsureCanTransitionFromPendingDutyOperationsManagerApprovalToApproved()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingDutyOperationsManagerApproval);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.PendingDutyOperationsManagerApproval,
            RestrictionAction.Approved,
            data);
    }

    [Fact]
    public void EnsureCanTransitionFromPendingDutyOperationsManagerApprovalToReturnedForChanges()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingDutyOperationsManagerApproval);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.PendingDutyOperationsManagerApproval,
            RestrictionAction.ReturnedForChanges,
            data);
    }

    [Fact]
    public void EnsureCanTransitionFromPendingDutyOperationsManagerApprovalToCancelledRefused()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingDutyOperationsManagerApproval);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.PendingDutyOperationsManagerApproval,
            RestrictionAction.CancelledRefused,
            data);
    }

    // ========== Approved State Transitions ==========

    [Fact]
    public void EnsureCanTransitionFromApprovedToPendingControlRoomSupervisorApproval()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.Approved);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.Approved,
            RestrictionAction.PendingControlRoomSupervisorApproval,
            data);
    }

    // ========== PendingControlRoomSupervisorApproval State Transitions ==========

    [Fact]
    public void EnsureCanTransitionFromPendingControlRoomSupervisorApprovalToActive()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingControlRoomSupervisorApproval);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.PendingControlRoomSupervisorApproval,
            RestrictionAction.Active,
            data);
    }

    // ========== ReturnedForChanges State Transitions ==========

    [Fact]
    public void EnsureCanTransitionFromReturnedForChangesToDraft()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.ReturnedForChanges);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.ReturnedForChanges,
            RestrictionAction.Resubmit,
            data);
    }

    // ========== Invalid Transitions ==========

    [Fact]
    public void EnsureCannotTransitionFromDraftToActiveDirectly()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.Draft);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.Draft,
                RestrictionAction.Active,
                data));

        Assert.Contains("Cannot transition from Draft", exception.Message);
    }

    [Fact]
    public void EnsureCannotTransitionFromApprovedToActiveDirectly()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.Approved);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.Approved,
                RestrictionAction.Active,
                data));

        Assert.Contains("Cannot transition from Approved", exception.Message);
    }

    [Fact]
    public void EnsureCannotTransitionFromPendingControlRoomSupervisorApprovalToDraft()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingControlRoomSupervisorApproval);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.PendingControlRoomSupervisorApproval,
                RestrictionAction.Resubmit,
                data));

        Assert.Contains("Cannot transition from PendingControlRoomSupervisorApproval", exception.Message);
    }

    [Fact]
    public void EnsureCanTransitionFromActiveToPendingCancellation()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.Active);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.Active,
            RestrictionAction.OriginatorStartCancellation,
            data);
    }

    [Fact]
    public void EnsureCanTransitionFromPendingCancellationToOutOfForce()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingCancellation);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.PendingCancellation,
            RestrictionAction.DutyOperationsManagerApproveCancellation,
            data);
    }

    [Fact]
    public void EnsureCanTransitionFromOutOfForceToAwaitingOperatorSignOut()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.OutOfForce);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.OutOfForce,
            RestrictionAction.ControlRoomCancellation,
            data);
    }

    [Fact]
    public void EnsureCanTransitionFromAwaitingOperatorSignOutToArchived()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.AwaitingOperatorSignOut);

        // Act & Assert
        _sut.EnsureCanTransition(
            RestrictionState.AwaitingOperatorSignOut,
            RestrictionAction.OperatorsArchive,
            data);
    }

    [Fact]
    public void EnsureCannotStartCancellationFromDraft()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.Draft);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.Draft,
                RestrictionAction.OriginatorStartCancellation,
                data));

        Assert.Contains("Cannot transition from Draft", exception.Message);
    }

    [Fact]
    public void EnsureCannotSkipDutyOperationsManagerCancellationApproval()
    {
        // Arrange - only the originator has started the cancellation
        var data = CreateValidDto(RestrictionState.PendingCancellation);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.PendingCancellation,
                RestrictionAction.ControlRoomCancellation,
                data));

        Assert.Contains("Cannot transition from PendingCancellation", exception.Message);
    }

    [Fact]
    public void EnsureCannotArchiveFromPendingCancellation()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.PendingCancellation);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.PendingCancellation,
                RestrictionAction.OperatorsArchive,
                data));

        Assert.Contains("Cannot transition from PendingCancellation", exception.Message);
    }

    [Fact]
    public void EnsureCannotArchiveFromOutOfForce()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.OutOfForce);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.OutOfForce,
                RestrictionAction.OperatorsArchive,
                data));

        Assert.Contains("Cannot transition from OutOfForce", exception.Message);
    }

    [Fact]
    public void EnsureCannotRestartCancellationAfterArchived()
    {
        // Arrange
        var data = CreateValidDto(RestrictionState.Archived);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _sut.EnsureCanTransition(
                RestrictionState.Archived,
                RestrictionAction.OriginatorStartCancellation,
                data));

        Assert.Contains("Cannot transition from Archived", exception.Message);
    }

    // ========== GetNextState Tests ==========

    [Fact]
    public void GetNextStateFromDraftToPendingDutyOperationsManagerApprovalReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.Draft,
            RestrictionAction.PendingDutyOperationsManagerApproval);

        // Assert
        Assert.Equal(RestrictionState.PendingDutyOperationsManagerApproval, nextState);
    }

    [Fact]
    public void GetNextStateFromPendingDutyOperationsManagerApprovalToApprovedReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.PendingDutyOperationsManagerApproval,
            RestrictionAction.Approved);

        // Assert
        Assert.Equal(RestrictionState.Approved, nextState);
    }

    [Fact]
    public void GetNextStateFromApprovedToPendingControlRoomSupervisorApprovalReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.Approved,
            RestrictionAction.PendingControlRoomSupervisorApproval);

        // Assert
        Assert.Equal(RestrictionState.PendingControlRoomSupervisorApproval, nextState);
    }

    [Fact]
    public void GetNextStateFromPendingControlRoomSupervisorApprovalToActiveReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.PendingControlRoomSupervisorApproval,
            RestrictionAction.Active);

        // Assert
        Assert.Equal(RestrictionState.Active, nextState);
    }

    [Fact]
    public void GetNextStateFromReturnedForChangesToDraftReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.ReturnedForChanges,
            RestrictionAction.Resubmit);

        // Assert
        Assert.Equal(RestrictionState.Draft, nextState);
    }

    [Fact]
    public void GetNextStateFromActiveToPendingCancellationReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.Active,
            RestrictionAction.OriginatorStartCancellation);

        // Assert
        Assert.Equal(RestrictionState.PendingCancellation, nextState);
    }

    [Fact]
    public void GetNextStateFromPendingCancellationToOutOfForceReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.PendingCancellation,
            RestrictionAction.DutyOperationsManagerApproveCancellation);

        // Assert
        Assert.Equal(RestrictionState.OutOfForce, nextState);
    }

    [Fact]
    public void GetNextStateFromOutOfForceToAwaitingOperatorSignOutReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.OutOfForce,
            RestrictionAction.ControlRoomCancellation);

        // Assert
        Assert.Equal(RestrictionState.AwaitingOperatorSignOut, nextState);
    }

    [Fact]
    public void GetNextStateFromAwaitingOperatorSignOutToArchivedReturnsCorrectState()
    {
        // Act
        var nextState = _sut.GetNextState(
            RestrictionState.AwaitingOperatorSignOut,
            RestrictionAction.OperatorsArchive);

        // Assert
        Assert.Equal(RestrictionState.Archived, nextState);
    }
}
