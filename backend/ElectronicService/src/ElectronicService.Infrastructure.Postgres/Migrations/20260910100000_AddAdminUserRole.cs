using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations;

[DbContext(typeof(ElectronicDbContext))]
[Migration("20260910100000_AddAdminUserRole")]
public partial class AddAdminUserRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE users
            SET type = 'Admin',
                updated_at_utc = NOW()
            WHERE id = (
                SELECT id
                FROM users
                WHERE type = 'Technical'
                ORDER BY created_at_utc, id
                LIMIT 1
            )
            AND NOT EXISTS (
                SELECT 1
                FROM users
                WHERE type = 'Admin'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE users
            SET type = 'Technical',
                updated_at_utc = NOW()
            WHERE id = (
                SELECT id
                FROM users
                WHERE type = 'Admin'
                ORDER BY updated_at_utc, created_at_utc, id
                LIMIT 1
            );
            """);
    }
}