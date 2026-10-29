namespace Backend.Models.Enums;

public enum RestrictionState
{
    Draft,
    PendingDutyOperationsManagerApproval,
    ReturnedForChanges,
    CancelledRefused,
    Approved,
    PendingControlRoomSupervisorApproval,
    Active,
    Empty,
    PendingCancellation,
    OutOfForce,
    AwaitingOperatorSignatures,
    AwaitingOperatorSignOut,
    Archived
}
