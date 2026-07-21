using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Repositories;

public sealed class ProductTypeSchemaRepository
    : IProductTypeSchemaRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ProductTypeSchemaRepository(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductType?>
        GetByCodeWithCharacteristicsAsync(
            string productTypeCode,
            CancellationToken cancellationToken = default)
    {
        var normalizedProductTypeCode =
            NormalizeProductTypeCode(productTypeCode);

        /*
         * AsNoTracking здесь намеренно отсутствует.
         * EF Core должен отслеживать ProductType
         * и новую сущность в его коллекции.
         */
        return _dbContext.ProductTypes
            .Include(productType =>
                productType.Characteristics)
            .FirstOrDefaultAsync(
                productType =>
                    productType.Code
                        == normalizedProductTypeCode,
                cancellationToken);
    }

    public Task<CharacteristicDefinition?>
        GetDefinitionByIdAsync(
            Guid characteristicDefinitionId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                definition =>
                    definition.Id
                        == characteristicDefinitionId,
                cancellationToken);
    }

    public Task<int> CountProductsWithoutCharacteristicAsync(
    Guid productTypeId,
    Guid characteristicDefinitionId,
    CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .CountAsync(
                product =>
                    product.ProductTypeId == productTypeId
                    && !_dbContext.ProductCharacteristics.Any(
                        productCharacteristic =>
                            productCharacteristic.ProductId
                                == product.Id
                            && productCharacteristic
                                .CharacteristicDefinitionId
                                == characteristicDefinitionId),
                cancellationToken);
    }

    public Task<int> CountProductsWithCharacteristicAsync(
    Guid productTypeId,
    Guid characteristicDefinitionId,
    CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .CountAsync(
                product =>
                    product.ProductTypeId == productTypeId
                    && _dbContext.ProductCharacteristics.Any(
                        productCharacteristic =>
                            productCharacteristic.ProductId
                                == product.Id
                            && productCharacteristic
                                .CharacteristicDefinitionId
                                == characteristicDefinitionId),
                cancellationToken);
    }

    public void MarkCharacteristicForRemoval(
        ProductTypeCharacteristic characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);

        _dbContext.ProductTypeCharacteristics.Remove(
            characteristic);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string NormalizeProductTypeCode(
        string productTypeCode)
    {
        return productTypeCode
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal)
            .Replace(
                " ",
                "_",
                StringComparison.Ordinal)
            .Replace(
                "-",
                "_",
                StringComparison.Ordinal);
    }
}