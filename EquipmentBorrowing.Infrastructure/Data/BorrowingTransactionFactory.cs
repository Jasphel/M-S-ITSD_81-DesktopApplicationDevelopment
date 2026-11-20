using System.Data;
using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EquipmentBorrowing.Infrastructure.Data;

/// <summary>
/// Creates one short-lived <see cref="BorrowingDbContext"/> per business transaction.
/// The three repositories handed out by the transaction all share that context and
/// that PostgreSQL transaction, so an issue/return commits (or rolls back) as one unit.
/// </summary>
public sealed class BorrowingTransactionFactory : IBorrowingTransactionFactory
{
    private readonly DbContextOptions<BorrowingDbContext> _options;

    public BorrowingTransactionFactory(DbContextOptions<BorrowingDbContext> options)
    {
        _options = options;
    }

    public async Task<IBorrowingTransaction> BeginAsync(CancellationToken cancellationToken = default)
    {
        var context = new BorrowingDbContext(_options);

        try
        {
            // Serializable: revalidation (active limit, availability) and the writes are
            // treated as if no one else changed the data in between (BR-15). A conflicting
            // concurrent transaction is rejected by PostgreSQL instead of silently winning.
            var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            return new BorrowingTransaction(context, transaction);
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }
}

internal sealed class BorrowingTransaction : IBorrowingTransaction
{
    // Controlled test seam for acceptance test A06/A18: when this environment variable is "1"
    // the transaction fails AFTER the writes were attempted and BEFORE the commit, so the
    // rollback can be demonstrated against a disposable database.
    public const string FailBeforeCommitVariable = "EBS_FAIL_BEFORE_COMMIT";

    private readonly BorrowingDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public BorrowingTransaction(BorrowingDbContext context, IDbContextTransaction transaction)
    {
        _context = context;
        _transaction = transaction;
        Students = new StudentRepository(context);
        Equipment = new EquipmentRepository(context);
        Borrowings = new BorrowingRepository(context);
    }

    public IStudentRepository Students { get; }
    public IEquipmentRepository Equipment { get; }
    public IBorrowingRepository Borrowings { get; }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (Environment.GetEnvironmentVariable(FailBeforeCommitVariable) == "1")
        {
            throw new DataAccessException(
                "Simulated failure before commit (" + FailBeforeCommitVariable + "=1).");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (DbSaveHelper.IsConflict(ex))
        {
            throw new DataConflictException("The transaction conflicted with another change.", ex);
        }
        catch (Exception ex)
        {
            throw new CommitOutcomeUnknownException(
                "The database did not confirm the commit, so the outcome is unknown.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Disposing an uncommitted transaction rolls it back.
        await _transaction.DisposeAsync();
        await _context.DisposeAsync();
    }
}
