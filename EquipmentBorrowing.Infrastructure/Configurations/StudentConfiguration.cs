using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentNumber)
            .IsRequired()
            .HasMaxLength(50);

        // Enforce unique student number in PostgreSQL (BR-01, BR-16)
        builder.HasIndex(s => s.StudentNumber)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ContactInfo)
            .IsRequired(false)
            .HasMaxLength(150);

        builder.Property(s => s.IsAllowedToBorrow)
            .IsRequired();
    }
}