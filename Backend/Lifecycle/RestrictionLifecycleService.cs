using Backend.Models.DTOs;
using Backend.Models.Enums;
using Backend.Models.Requests;
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

    public void EnsureCanCreate(CreateRestrictionRequest data)
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

    public void EnsureCanTransition(
        RestrictionState currentState,
        RestrictionAction action,
        OperationalRestrictionDto data)
    {
        if (!RestrictionLifecycle.Transitions.TryGetValue(action, out var transition))
        {
            throw new InvalidOperationException(
                $"No transition found for action: {action}");
        }

        if (transition.From != currentState)
        {
            throw new InvalidOperationException(
                $"Cannot transition from {currentState} using action {action}. " +
                $"Expected from state: {transition.From}");
        }

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

    public RestrictionState GetNextState(
        RestrictionState currentState,
        RestrictionAction action)
    {
        if (!RestrictionLifecycle.Transitions.TryGetValue(action, out var transition))
        {
            throw new InvalidOperationException(
                $"No transition found for action: {action}");
        }

        if (transition.From != currentState)
        {
            throw new InvalidOperationException(
                $"Cannot transition from {currentState} using action {action}. " +
                $"Expected from state: {transition.From}");
        }

        return transition.To;
    }
}
