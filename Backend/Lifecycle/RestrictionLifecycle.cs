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
    public static readonly IReadOnlyDictionary<
        RestrictionAction,
        RestrictionTransition> Transitions =
        new Dictionary<RestrictionAction, RestrictionTransition>
        {
            [RestrictionAction.CreateDraft] = new(
                RestrictionAction.CreateDraft,
                From: null,
                To: RestrictionState.Draft,
                Guards: new[]
                {
                    "CreationDataIsValid"
                }),
            
            // missing transitions from draft to active

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
            
            [RestrictionAction.OperatorsArchive] = new(
                RestrictionAction.OperatorsArchive,
                From: RestrictionState.AwaitingOperatorSignOut,
                To:   RestrictionState.Archived,
                Guards: new[]
                {
                    "OperatorSignoutsFound"
                }),
        };
}
