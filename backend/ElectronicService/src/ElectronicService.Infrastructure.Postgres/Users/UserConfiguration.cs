using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ElectronicService.Infrastructure.Postgres.Users;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        var displayNameConverter = new ValueConverter<UserDisplayName, string>(
            displayName => displayName.Value,
            value => UserDisplayName.Create(value).Value);

        var emailConverter = new ValueConverter<Email?, string?>(
            email => email == null ? null : email.Value,
            value => value == null ? null : Email.Create(value).Value);

        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint(
                "ck_users_type_not_none",
                "\"type\" <> 'None'");

            table.HasCheckConstraint(
                "ck_users_status_not_none",
                "\"status\" <> 'None'");
        });

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .HasConversion(displayNameConverter)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .HasConversion(emailConverter);

        builder.Property(user => user.Type)
            .HasColumnName("type")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(user => user.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}