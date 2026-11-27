using EquipmentBorrowing.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EquipmentBorrowing.Infrastructure.Data;

/// <summary>
/// Single place that translates EF Core / PostgreSQL failures into the
/// Application layer's exception types, and keeps long-lived contexts clean.
/// </summary>
internal static class DbSaveHelper
{
    // 23505 unique violation, 23503 foreign-key violation,
    // 40001 serialization failure, 40P01 deadlock detected.
    private static readonly string[] ConflictStates = { "23505", "23503", "40001", "40P01" };

    public static async Task SaveAsync(BorrowingDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DataConflictException("The record was changed or removed by someone else.", ex);
        }
        catch (DbUpdateException ex)
        {
            if (IsConflict(ex))
            {
                throw new DataConflictException("The change conflicts with existing data.", ex);
            }

            var detail = ex.InnerException?.Message ?? ex.Message;
            throw new DataAccessException("The change could not be saved.", ex);
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }

    public static bool IsConflict(Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is DbUpdateConcurrencyException)
            {
                return true;
            }

            if (current is PostgresException postgres &&
                Array.IndexOf(ConflictStates, postgres.SqlState) >= 0)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}