using Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Backend.Data;

public class OperationalRestrictionRepository : IOperationalRestrictionRepository
{
    private readonly AppDbContext _context;

    public OperationalRestrictionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OperationalRestriction restriction)
    {
        await _context.OperationalRestrictions.AddAsync(restriction);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OperationalRestriction restriction)
    {
        _context.OperationalRestrictions.Update(restriction);
        await _context.SaveChangesAsync();
    }

    public async Task<OperationalRestriction?> GetByIdAsync(Guid id)
    {
        return await _context.OperationalRestrictions.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RestrictionId == id);
    }

    public async Task<List<OperationalRestriction>> GetAllAsync(
        Expression<Func<OperationalRestriction, bool>> predicate)
    {
        return await _context.OperationalRestrictions
            .Where(predicate).AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<OperationalRestriction>> GetAllAsync()
    {
        return await _context.OperationalRestrictions.AsNoTracking().ToListAsync();
    }
}
