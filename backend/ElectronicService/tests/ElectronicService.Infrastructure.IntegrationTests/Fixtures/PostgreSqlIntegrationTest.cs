using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElectronicService.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Базовый класс PostgreSQL integration-тестов.
/// Перед каждым тестом создаёт новый DbContext и транзакцию,
/// после теста откатывает изменения и освобождает ресурсы.
/// </summary>
public abstract class PostgreSqlIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    private ElectronicDbContext? _dbContext;

    private IDbContextTransaction? _transaction;

    protected PostgreSqlIntegrationTest(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    protected ElectronicDbContext DbContext =>
        _dbContext
        ?? throw new InvalidOperationException(
            "DbContext ещё не инициализирован для integration-теста.");

    public async ValueTask InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();

        _transaction = await _dbContext.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(
                TestContext.Current.CancellationToken);

            await _transaction.DisposeAsync();
        }

        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Добавляет полный связанный граф каталога и сохраняет его
    /// в текущей тестовой транзакции.
    /// </summary>
    protected async Task SaveGraphAsync(CatalogProductGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        DbContext.Manufacturers.Add(graph.Manufacturer);
        DbContext.CharacteristicDefinitions.Add(graph.Definition);
        DbContext.ProductTypes.Add(graph.ProductType);
        DbContext.Products.Add(graph.Product);

        await DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);
    }
}
