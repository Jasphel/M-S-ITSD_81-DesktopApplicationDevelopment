using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Security;
using System.Xml.Linq;

namespace EquipmentBorrowing.Infrastructure.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
    //"This class contains the database mapping rules for the Student domain class."
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(student => student.Id);
        //Id → PRIMARY KEY

        builder.Property(student => student.Name)
            .IsRequired()
            .HasMaxLength(100);
//        Name
        //├── NOT NULL
        //└── maximum 100 characters

        builder.Property(student => student.IsAllowedToBorrow)
            .IsRequired();
        //makes the borrowing - permission value required.
    }
}