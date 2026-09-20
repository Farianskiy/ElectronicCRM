using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations;

[DbContext(typeof(ElectronicDbContext))]
[Migration("20260910140000_AddSystemDeveloperAndAdministratorRoles")]
public partial class AddSystemDeveloperAndAdministratorRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE users SET type = 'SystemDeveloper', updated_at_utc = NOW() WHERE type = 'Admin';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE users SET type = 'Admin', updated_at_utc = NOW() WHERE type = 'SystemDeveloper';");
    }
}