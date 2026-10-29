using Backend.Models.Entities;
using Backend.Models.Enums;
using System.Linq.Expressions;

namespace Backend.Data;

public interface IOperationalRestrictionRepository
{
    Task AddAsync(OperationalRestriction restriction);
    Task UpdateAsync(OperationalRestriction restriction);
    Task<OperationalRestriction?> GetByIdAsync(Guid id);
    Task<List<OperationalRestriction>> GetAllAsync(Expression<Func<OperationalRestriction, bool>> predicate);
    Task<List<OperationalRestriction>> GetAllAsync();
}