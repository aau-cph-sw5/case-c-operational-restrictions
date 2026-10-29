
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
                new CancelledRefused()
            });
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
            State = state
        };
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
}
