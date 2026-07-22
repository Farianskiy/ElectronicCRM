using ElectronicService.Domain.Catalog.Characteristics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Configurations;

public sealed class CharacteristicDefinitionConfiguration
    : IEntityTypeConfiguration<CharacteristicDefinition>
{
    public void Configure(EntityTypeBuilder<CharacteristicDefinition> builder)
    {
        builder.ToTable("characteristic_definitions", table =>
        {
            table.HasCheckConstraint(
                "ck_characteristic_definitions_data_type_not_none",
                "\"data_type\" <> 'None'");
        });

        builder.HasKey(characteristic => characteristic.Id);

        builder.Property(characteristic => characteristic.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(characteristic => characteristic.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(characteristic => characteristic.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(characteristic => characteristic.DataType)
            .HasColumnName("data_type")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(characteristic => characteristic.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50);

        builder.HasIndex(characteristic => characteristic.Code)
            .IsUnique();

        builder.HasIndex(characteristic => characteristic.Name);
    }
}