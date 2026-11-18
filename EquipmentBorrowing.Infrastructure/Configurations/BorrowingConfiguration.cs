using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.StudentId)
            .IsRequired();

        builder.Property(b => b.EquipmentId)
            .IsRequired();

        builder.Property(b => b.DateBorrowed)
            .IsRequired();

        builder.Property(b => b.ExpectedReturnDate)
            .IsRequired();

        builder.Property(b => b.ActualReturnDate)
            .IsRequired(false);

        builder.Property(b => b.Status)
            .IsRequired();

        // Enforce foreign keys and prevent hard deletes of referenced records (BR-14, BR-16)
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(b => b.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enforce single active borrowing per equipment item in PostgreSQL (BR-10, BR-16)
        // Note: BorrowingStatus.Active == 0
        builder.HasIndex(b => b.EquipmentId)
            .IsUnique()
            .HasFilter("\"Status\" = 0");
    }
}