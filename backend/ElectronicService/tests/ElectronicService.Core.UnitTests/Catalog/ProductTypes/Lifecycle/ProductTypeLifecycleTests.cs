using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using ElectronicService.Core.Catalog.ProductTypes.Lifecycle;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.UnitTests.Catalog.ProductTypes.Lifecycle;

public sealed class ProductTypeLifecycleTests
{
    [Fact]
    public async Task ArchiveKeepsEmptyTypeAndMarksItArchived()
    {
        var productType = ProductType.Create("EMPTY", "Пустой тип").Value;
        var repository = new FakeRepository(productType);
        var planner = new ProductTypeArchivePlanner(repository);
        var handler = new ArchiveProductTypeCommandHandler(planner, repository);

        var result = await handler.Handle(
            productType.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(productType.IsArchived);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    [Fact]
    public async Task ArchiveIsBlockedWhenTypeHasActiveProducts()
    {
        var productType = ProductType.Create("USED", "Используемый тип").Value;
        var repository = new FakeRepository(productType)
        {
            Dependencies = EmptyDependencies() with { ProductsCount = 7 }
        };
        var planner = new ProductTypeArchivePlanner(repository);
        var handler = new ArchiveProductTypeCommandHandler(planner, repository);

        var result = await handler.Handle(
            productType.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.False(productType.IsArchived);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    [Fact]
    public async Task RestoreMakesArchivedTypeActiveAgain()
    {
        var productType = ProductType.Create("ARCHIVED", "Архивный тип").Value;
        productType.Archive();
        var repository = new FakeRepository(productType);
        var handler = new RestoreProductTypeCommandHandler(repository);

        var result = await handler.Handle(
            productType.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(productType.IsArchived);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    private static ProductTypeKindChangeDependencySnapshot EmptyDependencies()
    {
        return new ProductTypeKindChangeDependencySnapshot(
            ProductsCount: 0,
            CharacteristicsCount: 0,
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
