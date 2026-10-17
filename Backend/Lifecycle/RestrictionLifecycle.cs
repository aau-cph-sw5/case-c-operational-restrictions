using Backend.Models.Enums;

namespace Backend.Lifecycle;

public enum RestrictionAction
{
    CreateDraft,
    OriginatorStartCancellation,
    DOMApproveCancellation,
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
                    "OperationalRestictionIdIsFound"
                }),

            // From Draft State to PendingDomApproval State
            [RestrictionAction.PendingDomApproval] = new(
                RestrictionAction.PendingDomApproval,
                From: RestrictionState.Draft,
                To: RestrictionState.PendingDomApproval,
                Guards: new[]
                {
                    "StartDateIsFound",
                    "DescriptionTextIsFound",
                    "OriginatorSignatureIsFound",
                    "OperationalRestictionIdIsFound"
                }),

            // From PendingDomApproval State to ReturnedForChanges State
            [RestrictionAction.ReturnForChanges] = new(
                RestrictionAction.ReturnForChanges,
                From: RestrictionState.PendingDomApproval,
                To: RestrictionState.ReturnForChanges,
                Guards: new[]
                {
                    "RequestedChangesByDom"
                }),

            // From PendingDomApproval State to CancelledRefused State
            [RestrictionAction.CancelledRefused] = new(
                RestrictionAction.CancelledRefused,
                From: RestrictionState.PendingDomApproval,
                To: RestrictionState.CancelledRefused,
                Guards: new[]
                {
                    "DomRefusesRestriction"
                }),

            // From PendingDomApproval State to Approved State
            [RestrictionAction.Approved] = new(
                RestrictionAction.Approved,
                From: RestrictionState.PendingDomApproval,
                To: RestrictionState.Approved,
                Guards: new[]
                {
                    "DomSignatureIsFound",
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
                    "DomSignatureIsFound",
                    "CRSignatureIsFound",
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
            
            [RestrictionAction.DOMApproveCancellation] = new(
                RestrictionAction.DOMApproveCancellation,
                From: RestrictionState.PendingCancellation,
                To:   RestrictionState.OutOfForce,
                Guards: new[]
                {
                    "DOMSignatureFound"
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
