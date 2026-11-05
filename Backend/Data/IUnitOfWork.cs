using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Data;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync();
}
