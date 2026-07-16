using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ElectronicService.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Управляет жизненным циклом временного PostgreSQL-контейнера.
/// Контейнер создаётся один раз для всей тестовой collection,
/// после запуска к базе применяются настоящие EF Core migrations.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("electronic_service_integration_tests")
            .WithUsername("electronic_integration_user")
            .WithPassword("electronic_integration_password")
            .Build();

    /// <summary>
    /// Создаёт новый DbContext, подключённый только к тестовому контейнеру.
    /// </summary>
    public ElectronicDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ElectronicDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .EnableDetailedErrors()
            .Options;

        return new ElectronicDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _container.StartAsync(cancellationToken);

        await using var dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
