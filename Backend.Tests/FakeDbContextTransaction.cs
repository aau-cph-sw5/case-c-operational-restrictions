using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Tests;
//records commits instead of talking to a database
public sealed class FakeDbContextTransaction : IDbContextTransaction
{
    private readonly Action _onCommit;

    public FakeDbContextTransaction(Action onCommit)
    {
        _onCommit = onCommit;
    }

    public Guid TransactionId { get; } = Guid.NewGuid();

    public void Commit() => _onCommit();

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _onCommit();
        return Task.CompletedTask;
    }

    public void Rollback() { }

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
