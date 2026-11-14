using Backend.Data;
using Backend.Models.Entities;

namespace Backend.Tests;
//in-memory stand-in for the audit event repository so the service tests
//needs no database
public class FakeRestrictionTransitionEventRepository : IRestrictionTransitionEventRepository
{
    public List<RestrictionTransitionEvent> Events { get; } = new();

    public Task AddAsync(RestrictionTransitionEvent transitionEvent)
    {
        Events.Add(transitionEvent);
        return Task.CompletedTask;
    }

    public Task<List<RestrictionTransitionEvent>> GetByOriginalRestrictionIdAsync(
        Guid originalRestrictionId)
    {
        return Task.FromResult(Events
            .Where(e => e.OriginalRestrictionId == originalRestrictionId)
            .OrderBy(e => e.OccurredAt)
            .ToList());
    }
}
