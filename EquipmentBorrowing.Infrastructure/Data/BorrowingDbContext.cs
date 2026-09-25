using EquipmentBorrowing.Domain;
//allows the database context to use your existing: Student, Equipment, Borrowing
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Data;

public class BorrowingDbContext : DbContext
{
    public BorrowingDbContext(DbContextOptions<BorrowingDbContext> options)
        //means "BorrowingDbContext is an Entity Framework database context."
        : base(options)
    {
    }
    //allows us to tell EF Core how the database should be configured


    //these three represents the tables in the database, and the type of data that will be stored in each table
    public DbSet<Student> Students { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<Borrowing> Borrowings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BorrowingDbContext).Assembly);
    }
//    This tells EF Core:
//"Look through this Infrastructure assembly and automatically find my IEntityTypeConfiguration<T> classes."
}