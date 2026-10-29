using Backend.Lifecycle;
using Backend.Models.Enums;
namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// Append-only audit record of a lifecycle transition (MET-C-002 / MET-C-021).
// Rows are only ever inserted, never updated or deleted.
public sealed class RestrictionTransitionEvent
{
    public Guid Id { get; init; }

    // The version row the transition produced.
    public Guid RestrictionVersionId { get; init; }

    // The first version of the restriction, shared by every version in the chain.
    public Guid OriginalRestrictionId { get; init; }

    public RestrictionAction Action { get; init; }

    // Null for the transition that creates the draft.
    public RestrictionState? FromState { get; init; }

    public RestrictionState ToState { get; init; }

    public Guid ActorId { get; init; }

    public DateTime OccurredAt { get; init; }
}
