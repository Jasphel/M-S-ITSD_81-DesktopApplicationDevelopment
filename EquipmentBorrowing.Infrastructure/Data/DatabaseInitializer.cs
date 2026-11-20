using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Infrastructure.Data;

/// <summary>
/// Startup bootstrap: applies pending migrations (this also creates the database
/// and every table on a clean PostgreSQL server) and inserts demo data ONLY when the
/// tables are empty, so an existing database is never overwritten (Lab 4 section VII).
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BorrowingDbContext>();

        await context.Database.MigrateAsync(cancellationToken);

        if (!await context.Students.AnyAsync(cancellationToken))
        {
            context.Students.AddRange(
                new Student(0, "2024-0001", "Claire", "claire@cmu.edu.ph", true),
                new Student(0, "2024-0002", "Jasper", "jasper@cmu.edu.ph", true),
                new Student(0, "2024-0003", "Mancawan", null, true),
                new Student(0, "2024-0004", "Jack", null, false)); // restricted, for testing
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.Equipment.AnyAsync(cancellationToken))
        {
            context.Equipment.AddRange(
                new Equipment(0, "EQ-1001", "Laptop", "Dell Latitude 14-inch"),
                new Equipment(0, "EQ-1002", "Projector", "Epson HDMI Portable Projector"),
                new Equipment(0, "EQ-1003", "HDMI Cable", "2-meter braided HDMI 2.1"),
                new Equipment(0, "EQ-1004", "DSLR Camera", "Canon EOS with 18-55mm lens"),
                new Equipment(0, "EQ-1005", "Tripod", "Heavy-duty aluminum tripod"));
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
