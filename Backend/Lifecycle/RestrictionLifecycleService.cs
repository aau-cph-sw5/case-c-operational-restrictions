using Backend.Models.DTOs;
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
