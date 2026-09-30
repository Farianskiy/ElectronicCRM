using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Catalog.Products.BulkUpdateProducts;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.TestCommon;

namespace ElectronicService.CatalogImport.UnitTests.Catalog.Products;

public sealed class BulkUpdateCatalogProductsCommandHandlerTests
{
    [Fact]
    public async Task HandleUpdatesMultipleProductsAndCreatesAuditEntries()
    {
        var productType = TestDataFactory.CreateProductType();
        var manufacturer = Manufacturer.Create("IEK").Value;
        var first = TestDataFactory.CreateProduct(
            article: "A-1",
            name: "Первый товар",
            productTypeId: productType.Id,
            manufacturerId: manufacturer.Id);
        var second = TestDataFactory.CreateProduct(
            article: "A-2",
            name: "Второй товар",
            productTypeId: productType.Id,
            manufacturerId: manufacturer.Id);
        var fixture = CreateFixture([first, second], [productType], [manufacturer]);

        var command = new BulkUpdateCatalogProductsCommand(
            Guid.NewGuid(),
            CanEditProducts: true,
            CanManagePrices: true,
            CanManageStock: true,
            [
                new BulkUpdateCatalogProductItem(
                    first.Id, "Первый товар обновлён", null, null, 1250m, "USD", 7m),
                new BulkUpdateCatalogProductItem(
                    second.Id, null, "A-2-NEW", null, null, null, null)
            ]);

        var result = await fixture.Handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.UpdatedProductIds.Count);
        Assert.Equal("Первый товар обновлён", first.Name.Value);
        Assert.Equal(1250m, first.Price.Amount);
        Assert.Equal("USD", first.Price.Currency);
        Assert.Equal(7m, first.StockQuantity.Value);
        Assert.Equal("A-2-NEW", second.Article.Value);
        Assert.Equal(1, fixture.Products.SaveCallsCount);
        Assert.Equal(2, fixture.Audit.Entries.Count);
        Assert.All(
            fixture.Audit.Entries,
            entry => Assert.Equal(ProductAuditOperation.BulkUpdated, entry.Operation));
    }

    [Fact]
    public async Task HandleValidatesEveryRowBeforeChangingAnyProduct()
    {
        var productType = TestDataFactory.CreateProductType();
        var manufacturer = Manufacturer.Create("IEK").Value;
        var first = TestDataFactory.CreateProduct(
            name: "Первый товар",
            productTypeId: productType.Id,
            manufacturerId: manufacturer.Id);
        var second = TestDataFactory.CreateProduct(
            article: "SECOND",
            name: "Второй товар",
            productTypeId: productType.Id,
            manufacturerId: manufacturer.Id);
        var fixture = CreateFixture([first, second], [productType], [manufacturer]);

        var command = new BulkUpdateCatalogProductsCommand(
            Guid.NewGuid(),
            true,
            true,
            true,
            [
                new BulkUpdateCatalogProductItem(
                    first.Id, "Не должно сохраниться", null, null, null, null, null),
                new BulkUpdateCatalogProductItem(
                    second.Id, null, null, null, null, null, -1m)
            ]);

        var result = await fixture.Handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Первый товар", first.Name.Value);
        Assert.Equal(0, fixture.Products.SaveCallsCount);
        Assert.Empty(fixture.Audit.Entries);
    }

    [Fact]
    public async Task HandleRejectsChangesWithoutRequiredPermission()
    {
        var handler = new BulkUpdateCatalogProductsCommandHandler(null!, null!, null!);
        var row = new BulkUpdateCatalogProductItem(
            Guid.NewGuid(), null, null, null, 10m, null, null);

        var result = await handler.Handle(
            new BulkUpdateCatalogProductsCommand(
                Guid.NewGuid(), true, false, true, [row]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.bulk.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task HandleRejectsMoreThanOneHundredRows()
    {
        var handler = new BulkUpdateCatalogProductsCommandHandler(null!, null!, null!);
        var rows = Enumerable.Range(0, 101)
            .Select(_ => new BulkUpdateCatalogProductItem(
                Guid.NewGuid(), null, null, null, null, null, 1m))
            .ToArray();

        var result = await handler.Handle(
            new BulkUpdateCatalogProductsCommand(
                Guid.NewGuid(), true, true, true, rows),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.bulk.limit_exceeded", result.Error.Code);
    }

    private static Fixture CreateFixture(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<ProductType> productTypes,
        IReadOnlyCollection<Manufacturer> manufacturers)
    {
        var productRepository = new ProductRepositoryFake(products);
        var metadataRepository = new MetadataRepositoryFake(productTypes, manufacturers);
        var auditRepository = new AuditRepositoryFake();
        var snapshotBuilder = new ProductAuditSnapshotBuilder(metadataRepository);
        var auditRecorder = new ProductAuditRecorder(auditRepository, snapshotBuilder);
        var handler = new BulkUpdateCatalogProductsCommandHandler(
            productRepository,
            metadataRepository,
            auditRecorder);

        return new Fixture(handler, productRepository, auditRepository);
    }

    private sealed record Fixture(
        BulkUpdateCatalogProductsCommandHandler Handler,
        ProductRepositoryFake Products,
        AuditRepositoryFake Audit);

    private sealed class ProductRepositoryFake : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products;

        public ProductRepositoryFake(IEnumerable<Product> products)
        {
            _products = products.ToDictionary(product => product.Id);
        }

        public int SaveCallsCount { get; private set; }

        public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.GetValueOrDefault(productId));

        public Task<Product?> GetByIdWithDetailsAsync(Guid productId, CancellationToken cancellationToken = default) =>
            GetByIdAsync(productId, cancellationToken);

        public Task<IReadOnlyCollection<Product>> GetByIdsWithDetailsAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Product>>(
                productIds.Where(_products.ContainsKey).Select(id => _products[id]).ToArray());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCallsCount++;
            return Task.CompletedTask;
        }

        public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    private sealed class MetadataRepositoryFake : ICatalogProductMetadataRepository
    {
        private readonly Dictionary<Guid, ProductType> _productTypes;
        private readonly Dictionary<Guid, Manufacturer> _manufacturers;

        public MetadataRepositoryFake(
            IEnumerable<ProductType> productTypes,
            IEnumerable<Manufacturer> manufacturers)
        {
            _productTypes = productTypes.ToDictionary(value => value.Id);
            _manufacturers = manufacturers.ToDictionary(value => value.Id);
        }

        public Task<ProductType?> GetProductTypeByIdAsync(Guid productTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_productTypes.GetValueOrDefault(productTypeId));

        public Task<IReadOnlyCollection<ProductType>> GetProductTypesByIdsAsync(IReadOnlyCollection<Guid> productTypeIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ProductType>>(productTypeIds.Where(_productTypes.ContainsKey).Select(id => _productTypes[id]).ToArray());

        public Task<CharacteristicDefinition?> GetCharacteristicDefinitionByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult<CharacteristicDefinition?>(null);

        public Task<Manufacturer?> GetManufacturerByIdAsync(Guid manufacturerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_manufacturers.GetValueOrDefault(manufacturerId));

        public Task<IReadOnlyCollection<CharacteristicDefinition>> GetCharacteristicDefinitionsByIdsAsync(IReadOnlyCollection<Guid> characteristicDefinitionIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CharacteristicDefinition>>([]);

        public Task<IReadOnlyCollection<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Manufacturer>>(_manufacturers.Values.ToArray());
    }

    private sealed class AuditRepositoryFake : IProductAuditRepository
    {
        public List<ProductAuditEntry> Entries { get; } = [];

        public void Add(ProductAuditEntry auditEntry) => Entries.Add(auditEntry);
    }
}
