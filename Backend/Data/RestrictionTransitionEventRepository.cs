using Backend.Models.Entities;

using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class RestrictionTransitionEventRepository : IRestrictionTransitionEventRepository
{
    private readonly AppDbContext _context;

    public RestrictionTransitionEventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RestrictionTransitionEvent transitionEvent)
    {
        await _context.RestrictionTransitionEvents.AddAsync(transitionEvent);
        await _context.SaveChangesAsync();
    }

    public async Task<List<RestrictionTransitionEvent>> GetByOriginalRestrictionIdAsync(
        Guid originalRestrictionId)
    {
        return await _context.RestrictionTransitionEvents
            .Where(e => e.OriginalRestrictionId == originalRestrictionId)
            .OrderBy(e => e.OccurredAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
