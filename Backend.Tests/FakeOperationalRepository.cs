using Backend.Data;
using Backend.Models.Entities;
using System.Linq.Expressions;

namespace Backend.Tests;
//in-memory stand-in for the real repository so the OperationalRestrictionServiceTests 
//needs no database
public class FakeOperationalRestrictionRepository : IOperationalRestrictionRepository
{
    public List<OperationalRestriction> Restrictions { get; } = new();
    public bool UpdateWasCalled { get; private set; }

    public Task AddAsync(OperationalRestriction restriction)
    {
        Restrictions.Add(restriction);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OperationalRestriction restriction)
    {
        UpdateWasCalled = true;
        return Task.CompletedTask;
    }

    public Task<OperationalRestriction?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(Restrictions.FirstOrDefault(
            r => r.RestrictionId == id));
    }

    public Task<List<OperationalRestriction>> GetAllAsync(
        Expression<Func<OperationalRestriction, bool>> predicate)
    {
        return Task.FromResult(Restrictions
            .Where(predicate.Compile())
            .ToList());
    }

    public Task<List<OperationalRestriction>> GetAllAsync()
    {
        return Task.FromResult(Restrictions.ToList());
    }
}
