using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectronicService.Infrastructure.Postgres.Users;

public sealed class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("user_permission_overrides");

        builder.HasKey(value => new
        {
            value.UserId,
            value.PermissionCode
        });

        builder.Property(value => value.UserId)
            .HasColumnName("user_id");

        builder.Property(value => value.PermissionCode)
            .HasColumnName("permission_code")
            .HasMaxLength(64)
            .HasConversion<string>();

        builder.Property(value => value.IsAllowed)
            .HasColumnName("is_allowed");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(value => value.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}