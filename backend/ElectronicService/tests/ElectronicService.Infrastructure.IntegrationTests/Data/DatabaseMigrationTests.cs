using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Data;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class DatabaseMigrationTests : PostgreSqlIntegrationTest
{
    public DatabaseMigrationTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет, что fixture применил реальные migrations:
    // в базе есть применённые migrations и не осталось ожидающих.
    [Fact]
    public async Task DatabaseHasNoPendingMigrations()
    {
        // Arrange
        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act
        var appliedMigrations =
            await DbContext.Database.GetAppliedMigrationsAsync(
                cancellationToken);

        var pendingMigrations =
            await DbContext.Database.GetPendingMigrationsAsync(
                cancellationToken);

        // Assert
        Assert.NotEmpty(appliedMigrations);
        Assert.Empty(pendingMigrations);
    }
}
