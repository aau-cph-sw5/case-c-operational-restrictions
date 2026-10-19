namespace Backend.Models.Enums;

public enum RestrictionState
{
    Draft,
    PendingDomApproval,
    ReturnedForChanges,
    CancelledRefused,
    Approved,
    Active,
    Empty,
    PendingCancellation,
    OutOfForce,
    AwaitingOperatorSignatures,
    AwaitingOperatorSignOut,
    Archived
}