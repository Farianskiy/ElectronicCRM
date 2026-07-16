using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Products;

namespace ElectronicService.Core.UnitTests.TestDoubles;

/// <summary>
/// Тестовая реализация репозитория товаров.
/// Хранит товары в памяти и записывает сведения о вызовах,
/// чтобы тесты могли проверить взаимодействие handler с репозиторием.
/// </summary>
internal sealed class FakeProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = [];

    public int GetByIdCallsCount { get; private set; }

    public int GetByIdWithDetailsCallsCount { get; private set; }

    public int SaveChangesCallsCount { get; private set; }

    public Guid? LastGetByIdProductId { get; private set; }

    public Guid? LastGetByIdWithDetailsProductId { get; private set; }

    public CancellationToken LastGetByIdCancellationToken { get; private set; }

    public CancellationToken LastGetByIdWithDetailsCancellationToken { get; private set; }

    public CancellationToken LastSaveChangesCancellationToken { get; private set; }

    /// <summary>
    /// Добавляет существующий товар в память fake-репозитория.
    /// Это подготовка тестовых данных, а не production-операция добавления товара.
    /// </summary>
    public void AddExisting(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        _products[product.Id] = product;
    }

    public Task<Product?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        GetByIdCallsCount++;
        LastGetByIdProductId = productId;
        LastGetByIdCancellationToken = cancellationToken;

        _products.TryGetValue(productId, out var product);

        return Task.FromResult<Product?>(product);
    }

    public Task<Product?> GetByIdWithDetailsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        GetByIdWithDetailsCallsCount++;
        LastGetByIdWithDetailsProductId = productId;
        LastGetByIdWithDetailsCancellationToken = cancellationToken;

        _products.TryGetValue(productId, out var product);

        return Task.FromResult<Product?>(product);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallsCount++;
        LastSaveChangesCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }
}
