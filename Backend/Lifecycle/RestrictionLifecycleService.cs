using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;

using System.Linq;

namespace Backend.Lifecycle;

public sealed class RestrictionLifecycleService
{
    private readonly Dictionary<string, IRestrictionGuard> _guards;

    public RestrictionLifecycleService(
        IEnumerable<IRestrictionGuard> guards)
    {
        _guards = guards.ToDictionary(g => g.Name);
    }

    public void EnsureCanCreate(CreateRestrictionDto data)
    {
        var transition =
            RestrictionLifecycle.Transitions[
                RestrictionAction.CreateDraft];

        foreach (var guardName in transition.Guards)
        {
            if (!_guards.TryGetValue(guardName, out var guard))
            {
                throw new InvalidOperationException(
                    $"Lifecycle guard '{guardName}' is not registered.");
            }

            if (!guard.IsSatisfied(data))
            {
                throw new InvalidOperationException(
                    $"Transition '{transition.Action}' refused: " +
                    $"guard '{guardName}' failed.");
            }
        }
    }

    public void EnsureCanTransition(CreateRestrictionDto data)
    {
        var transition =
            RestrictionLifecycle.Transitions[
                RestrictionAction.CreateDraft];

        /*
        var transition = data.RestrictionState switch
        {
            State.Active => RestrictionLifecycle.Transitions[
                RestrictionAction.OriginatorStartCancellation],

            State.PendingCancellation => RestrictionLifecycle.Transitions[
                RestrictionAction.DOMApproveCancellation],

            State.OutOfForce => RestrictionLifecycle.Transitions[
                RestrictionAction.ControlRoomCancellation],

            State.AwaitingOperatorSignOut => RestrictionLifecycle.Transitions[
                RestrictionAction.OperatorsArchive],

            _ => throw new InvalidOperationException(
                $"No matching transition found for state: {data.RestrictionState}")
        }; */


        foreach (var guardName in transition.Guards)
        {
            if (!_guards.TryGetValue(guardName, out var guard))
            {
                throw new InvalidOperationException(
                    $"Lifecycle guard '{guardName}' is not registered.");
            }

            if (!guard.IsSatisfied(data))
            {
                throw new InvalidOperationException(
                    $"Transition '{transition.Action}' refused: " +
                    $"guard '{guardName}' failed.");
            }
        }
    }
}
