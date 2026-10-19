using Backend.Models.Enums;

namespace Backend.Lifecycle;

public enum RestrictionAction
{
    CreateDraft,
    OriginatorStartCancellation,
    DutyOperationsManagerApproveCancellation,
    ReturnedForChanges,
    ControlRoomCancellation,
    PendingDutyOperationsManagerApproval,
    CancelledRefused,
    OperatorsArchive,
    Approved,
    Active
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
                    "OperationalRestictionIdIsFound"
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
                    "RequestedChangesByDutyOperationsManager"
                }),

            // From PendingDutyOperationsManagerApproval State to CancelledRefused State
            [RestrictionAction.CancelledRefused] = new(
                RestrictionAction.CancelledRefused,
                From: RestrictionState.PendingDutyOperationsManagerApproval,
                To: RestrictionState.CancelledRefused,
                Guards: new[]
                {
                    "DutyOperationsManagerRefusesRestriction"
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
            
            // From Approved State to Active State
            [RestrictionAction.Active] = new(
                RestrictionAction.Active,
                From: RestrictionState.Approved,
                To: RestrictionState.Active,
                Guards: new[]
                {
                    "OriginatorSignatureIsFound",
                    "DutyOperationsManagerSignatureIsFound",
                    "ControlRoomSupervisorSignatureIsFound",
                    "OperatorsAreAssigned"
                }),
            
            /*// missing transitions from draft to active ACTIVE
            [RestrictionAction.Active] = new(
                RestrictionAction.Active,
                From: RestrictionState.Draft,
                To:   RestrictionState.Active,
                Guards: new[]
                {
                    "OriginatorSignatureFound"
                }),

            [RestrictionAction.OriginatorStartCancellation] = new(
                RestrictionAction.OriginatorStartCancellation,
                From: RestrictionState.Active,
                To:   RestrictionState.PendingCancellation,
                Guards: new[]
                {
                    "OriginatorSignatureFound"
                }),
            
            [RestrictionAction.DutyOperationsManagerApproveCancellation] = new(
                RestrictionAction.DutyOperationsManagerApproveCancellation,
                From: RestrictionState.PendingCancellation,
                To:   RestrictionState.OutOfForce,
                Guards: new[]
                {
                    "DutyOperationsManagerSignatureFound"
                }),

            [RestrictionAction.ControlRoomCancellation] = new(
                RestrictionAction.ControlRoomCancellation,
                From: RestrictionState.OutOfForce,
                To:   RestrictionState.AwaitingOperatorSignOut,
                Guards: new[]
                {
                    "ControlRoomSignatureFound"
                }),
            
            /*
            [RestrictionAction.OperatorsArchive] = new(
                RestrictionAction.OperatorsArchive,
                From: RestrictionState.AwaitingOperatorSignOut,
                To:   RestrictionState.Archived,
                Guards: new[]
                {
                    "OperatorSignoutsFound"
                }),
            */
        };
}
