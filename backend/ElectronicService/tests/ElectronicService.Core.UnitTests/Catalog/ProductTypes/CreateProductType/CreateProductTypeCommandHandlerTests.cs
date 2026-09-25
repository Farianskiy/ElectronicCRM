using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.CreateProductType;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.UnitTests.Catalog.ProductTypes.CreateProductType;

public sealed class CreateProductTypeCommandHandlerTests
{
    [Fact]
    public async Task HandleCreatesNormalizedProductTypeAndSavesChanges()
    {
        var repository = new FakeProductTypeManagementRepository();
        var handler = new CreateProductTypeCommandHandler(repository);
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await handler.Handle(
            new CreateProductTypeCommand(
                " modular-breaker ",
                "  Модульный автомат  "),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("MODULAR_BREAKER", result.Value.Code);
        Assert.Equal("Модульный автомат", result.Value.Name);
        Assert.Equal(result.Value.Id, repository.AddedProductType?.Id);
        Assert.Equal(cancellationToken, repository.LastCancellationToken);
        Assert.Equal(1, repository.SaveChangesCallsCount);
    }

    [Fact]
    public async Task HandleRejectsDuplicateNormalizedCode()
    {
        var repository = new FakeProductTypeManagementRepository
        {
            ProductTypeExists = true
        };

        var handler = new CreateProductTypeCommandHandler(repository);

        var result = await handler.Handle(
            new CreateProductTypeCommand(
                "modular-breaker",
                "Модульный автомат"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.product_type.already_exists",
            result.Error.Code);
        Assert.Equal("MODULAR_BREAKER", repository.LastCheckedCode);
        Assert.Null(repository.AddedProductType);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    [Fact]
    public async Task HandleRejectsEmptyCodeBeforeRepositoryCall()
    {
        var repository = new FakeProductTypeManagementRepository();
        var handler = new CreateProductTypeCommandHandler(repository);

        var result = await handler.Handle(
            new CreateProductTypeCommand(" ", "Модульный автомат"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
        Assert.Null(repository.LastCheckedCode);
        Assert.Null(repository.AddedProductType);
        Assert.Equal(0, repository.SaveChangesCallsCount);
    }

    private sealed class FakeProductTypeManagementRepository
        : IProductTypeManagementRepository
    {
        public bool ProductTypeExists { get; init; }

        public string? LastCheckedCode { get; private set; }

        public ProductType? AddedProductType { get; private set; }

        public int SaveChangesCallsCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<bool> ExistsByCodeAsync(
            string normalizedCode,
            CancellationToken cancellationToken = default)
        {
            LastCheckedCode = normalizedCode;
            LastCancellationToken = cancellationToken;

            return Task.FromResult(ProductTypeExists);
        }

        public void Add(ProductType productType)
        {
            AddedProductType = productType;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallsCount++;
            LastCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }
    }
}
