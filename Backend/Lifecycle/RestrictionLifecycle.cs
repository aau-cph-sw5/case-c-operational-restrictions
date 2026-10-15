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
                From: State.Active,
                To:   State.PendingCancellation,
                Guards: new[]
                {
                    "OriginatorSignatureFound"
                }),
            
            [RestrictionAction.DOMApproveCancellation] = new(
                RestrictionAction.DOMApproveCancellation,
                From: State.PendingCancellation,
                To:   State.OutOfForce,
                Guards: new[]
                {
                    "DOMSignatureFound"
                }),

            [RestrictionAction.ControlRoomCancellation] = new(
                RestrictionAction.ControlRoomCancellation,
                From: State.OutOfForce,
                To:   State.AwaitingOperatorSignOut,
                Guards: new[]
                {
                    "ControlRoomSignatureFound"
                }),
            
            [RestrictionAction.OperatorsArchive] = new(
                RestrictionAction.OperatorsArchive,
                From: State.AwaitingOperatorSignOut,
                To:   State.Archived,
                Guards: new[]
                {
                    "OperatorSignoutsFound"
                }),
        };
}
