using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.UnitTests.Catalog.ProductTypes.ChangeKind;

public sealed class ProductTypeKindChangeTests
{
    [Fact]
    public async Task PreviewAllowsComponentWithoutDependenciesToBecomeMainProduct()
    {
        var productType = ProductType.Create(
            "CONTACTOR_KTI",
            "Контактор КТИ",
            ProductTypeKind.Component).Value;
        var repository = new FakeRepository(productType)
        {
            Dependencies = EmptyDependencies(productsCount: 7, characteristicsCount: 3)
        };
        var planner = new ProductTypeKindChangePlanner(repository);

        var result = await planner.BuildAsync(
            productType.Id,
            ProductTypeKind.MainProduct,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CanChange);
        Assert.Equal(7, result.Value.ProductsCount);
        Assert.Equal(3, result.Value.CharacteristicsCount);
        Assert.Empty(result.Value.Blockers);
    }

    [Fact]
    public async Task PreviewBlocksComponentUsedByCompatibilityRules()
    {
        var productType = ProductType.Create(
            "PKI",
            "ПКИ",
            ProductTypeKind.Component).Value;
        var repository = new FakeRepository(productType)
        {
            Dependencies = EmptyDependencies() with
            {
                ComponentOffersCount = 2
            }
        };
        var planner = new ProductTypeKindChangePlanner(repository);

        var result = await planner.BuildAsync(
            productType.Id,
            ProductTypeKind.MainProduct,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.CanChange);
        var blocker = Assert.Single(result.Value.Blockers);
        Assert.Equal("component_offers", blocker.Code);
        Assert.Equal(2, blocker.Count);
    }

    [Fact]
    public async Task ApplyChangesKindAndSavesWhenPreviewHasNoBlockers()
    {
        var productType = ProductType.Create(
            "CONTACTOR_KTI",
            "Контактор КТИ",
            ProductTypeKind.Component).Value;
        var repository = new FakeRepository(productType)
        {
            Dependencies = EmptyDependencies(productsCount: 7, characteristicsCount: 3)
        };
        var planner = new ProductTypeKindChangePlanner(repository);
        var handler = new ChangeProductTypeKindCommandHandler(planner, repository);

        var result = await handler.Handle(
            new ChangeProductTypeKindCommand(
                productType.Id,
                ProductTypeKind.MainProduct),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductTypeKind.MainProduct, productType.Kind);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    [Fact]
    public async Task ApplyDoesNotChangeKindWhenDependenciesBlockIt()
    {
        var productType = ProductType.Create(
            "PKI",
            "ПКИ",
            ProductTypeKind.Component).Value;
        var repository = new FakeRepository(productType)
        {
            Dependencies = EmptyDependencies() with
            {
                SelectedComponentsCount = 1
            }
        };
        var planner = new ProductTypeKindChangePlanner(repository);
        var handler = new ChangeProductTypeKindCommandHandler(planner, repository);

        var result = await handler.Handle(
            new ChangeProductTypeKindCommand(
                productType.Id,
                ProductTypeKind.MainProduct),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.product_type.kind_change_is_blocked",
            result.Error.Code);
        Assert.Equal(ProductTypeKind.Component, productType.Kind);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    private static ProductTypeKindChangeDependencySnapshot EmptyDependencies(
        int productsCount = 0,
        int characteristicsCount = 0)
    {
        return new ProductTypeKindChangeDependencySnapshot(
            productsCount,
            characteristicsCount,
            ComponentOffersCount: 0,
            SelectedComponentsCount: 0,
            DraftCalculationComponentLinesCount: 0,
            ComponentNeedsCount: 0,
            DraftMainCalculationLinesCount: 0,
            ActiveImportBatchesCount: 0);
    }

    private sealed class FakeRepository : IProductTypeKindManagementRepository
    {
        private readonly ProductType _productType;

        public FakeRepository(ProductType productType)
        {
            _productType = productType;
        }

        public ProductTypeKindChangeDependencySnapshot Dependencies { get; init; } =
            EmptyDependencies();

        public int SaveChangesCallsCount { get; private set; }

        public Task<ProductType?> GetByIdAsync(
            Guid productTypeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProductType?>(
                productTypeId == _productType.Id ? _productType : null);
        }

        public Task<ProductTypeKindChangeDependencySnapshot> GetDependenciesAsync(
            Guid productTypeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Dependencies);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallsCount++;
            return Task.CompletedTask;
        }
    }
}
