using Backend.Models.Enums;

namespace Backend.Lifecycle;

public enum RestrictionAction
{
    CreateDraft,
    ReturnedForChanges,
    Resubmit,
    PendingDutyOperationsManagerApproval,
    CancelledRefused,
    Approved,
    PendingControlRoomSupervisorApproval,
    Active,
    OriginatorStartCancellation,
    DutyOperationsManagerApproveCancellation,
    ControlRoomCancellation,
    OperatorsArchive
}

public sealed record RestrictionTransition(
    RestrictionAction Action,
    RestrictionState? From,
    RestrictionState To,
    IReadOnlyList<string> Guards);

public static class RestrictionLifecycle
{
    //States
    public static readonly IReadOnlyDictionary<
        RestrictionAction,
        RestrictionTransition> Transitions =
        new Dictionary<RestrictionAction, RestrictionTransition>

        
        {
            // Draft state 
            [RestrictionAction.CreateDraft] = new(
                RestrictionAction.CreateDraft,
                From: null,
                To: RestrictionState.Draft,
                Guards: new[]
                {
                    "StartDateIsFound",
                }),

            // From Draft State to PendingDutyOperationsManagerApproval State
            [RestrictionAction.PendingDutyOperationsManagerApproval] = new(
                RestrictionAction.PendingDutyOperationsManagerApproval,
                From: RestrictionState.Draft,
                To: RestrictionState.PendingDutyOperationsManagerApproval,
                Guards: new[]
                {
                    "StartDateIsFound",
                    "DescriptionTextIsFound",
                    "OriginatorSignatureIsFound",
                    "OperationalRestictionIdIsFound"
                }),

            // From PendingDutyOperationsManagerApproval State to ReturnedForChanges State
            [RestrictionAction.ReturnedForChanges] = new(
                RestrictionAction.ReturnedForChanges,
                From: RestrictionState.PendingDutyOperationsManagerApproval,
                To: RestrictionState.ReturnedForChanges,
                Guards: new[]
                {
                    "ReturnedForChanges"
                }),

            // From ReturnedForChanges State back to Draft State
            [RestrictionAction.Resubmit] = new(
                RestrictionAction.Resubmit,
                From: RestrictionState.ReturnedForChanges,
                To: RestrictionState.Draft,
                Guards: new[]
                {
                    "OperationalRestictionIdIsFound"
                }),

            // From PendingDutyOperationsManagerApproval State to CancelledRefused State
            [RestrictionAction.CancelledRefused] = new(
                RestrictionAction.CancelledRefused,
                From: RestrictionState.PendingDutyOperationsManagerApproval,
                To: RestrictionState.CancelledRefused,
                Guards: new[]
                {
                    "CancelledRefused"
                }),

            // From PendingDutyOperationsManagerApproval State to Approved State
            [RestrictionAction.Approved] = new(
                RestrictionAction.Approved,
                From: RestrictionState.PendingDutyOperationsManagerApproval,
                To: RestrictionState.Approved,
                Guards: new[]
                {
                    "DutyOperationsManagerSignatureIsFound",
                    "OriginatorSignatureIsFound"
                }),

            // From Approved State to PendingControlRoomSupervisorApproval State
            [RestrictionAction.PendingControlRoomSupervisorApproval] = new(
                RestrictionAction.PendingControlRoomSupervisorApproval,
                From: RestrictionState.Approved,
                To: RestrictionState.PendingControlRoomSupervisorApproval,
                Guards: new[]
                {
                    "DutyOperationsManagerSignatureIsFound",
                    "OriginatorSignatureIsFound"
                }),

            // From PendingControlRoomSupervisorApproval State to Active State
            [RestrictionAction.Active] = new(
                RestrictionAction.Active,
                From: RestrictionState.PendingControlRoomSupervisorApproval,
                To: RestrictionState.Active,
                Guards: new[]
                {
                    "OriginatorSignatureIsFound",
                    "DutyOperationsManagerSignatureIsFound",
                    "ControlRoomSupervisorSignatureIsFound",
                    "OperatorsAreAssigned"
                }),

            // Cancellation Transitions - Group 1:

            [RestrictionAction.OriginatorStartCancellation] = new(
                RestrictionAction.OriginatorStartCancellation,
                From: RestrictionState.Active,
                To:   RestrictionState.PendingCancellation,
                Guards: new[]
                {
                    "OriginatorCancellationSignatureIsFound"
                }),
            
            [RestrictionAction.DutyOperationsManagerApproveCancellation] = new(
                RestrictionAction.DutyOperationsManagerApproveCancellation,
                From: RestrictionState.PendingCancellation,
                To:   RestrictionState.OutOfForce,
                Guards: new[]
                {   
                    "OriginatorCancellationSignatureIsFound",
                    "DutyOperationsManagerCancellationSignatureIsFound"
                }),

            [RestrictionAction.ControlRoomCancellation] = new(
                RestrictionAction.ControlRoomCancellation,
                From: RestrictionState.OutOfForce,
                To:   RestrictionState.AwaitingOperatorSignOut,
                Guards: new[]
                {
                    "OriginatorCancellationSignatureIsFound",
                    "DutyOperationsManagerCancellationSignatureIsFound",
                    "ControlRoomCancellationSignatureIsFound"
                }),
            
            [RestrictionAction.OperatorsArchive] = new(
                RestrictionAction.OperatorsArchive,
                From: RestrictionState.AwaitingOperatorSignOut,
                To:   RestrictionState.Archived,
                Guards: new[]
                {
                    "OriginatorCancellationSignatureIsFound",
                    "DutyOperationsManagerCancellationSignatureIsFound",
                    "ControlRoomCancellationSignatureIsFound",
                    "OperatorCancellationSignaturesAreFound"
                }),
        };
}
