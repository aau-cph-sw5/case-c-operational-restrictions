using Backend.Data;

using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Tests;
//no-op stand-in for the unit of work so the service tests
//needs no database transaction
public class FakeUnitOfWork : IUnitOfWork
{
    public int CommittedTransactions { get; private set; }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return Task.FromResult<IDbContextTransaction>(
            new FakeDbContextTransaction(() => CommittedTransactions++));
    }
}
