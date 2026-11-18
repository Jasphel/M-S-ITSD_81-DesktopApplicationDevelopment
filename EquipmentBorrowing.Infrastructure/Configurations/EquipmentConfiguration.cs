using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentBorrowing.Infrastructure.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Identifier)
            .IsRequired()
            .HasMaxLength(50);

        // Enforce unique equipment identifier in PostgreSQL (BR-05, BR-16)
        builder.HasIndex(e => e.Identifier)
            .IsUnique();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired(false)
            .HasMaxLength(300);

        builder.Property(e => e.IsAvailable)
            .IsRequired();
    }
}