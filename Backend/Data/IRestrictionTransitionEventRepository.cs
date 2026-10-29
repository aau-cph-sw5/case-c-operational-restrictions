using Backend.Models.Entities;

namespace Backend.Data;

// Append-only: there is deliberately no update or delete.
public interface IRestrictionTransitionEventRepository
{
    Task AddAsync(RestrictionTransitionEvent transitionEvent);
    Task<List<RestrictionTransitionEvent>> GetByOriginalRestrictionIdAsync(Guid originalRestrictionId);
}
