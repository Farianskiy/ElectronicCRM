using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.Import;

public interface ICatalogPriceListProcessor
{
    Task<Result<CatalogPriceListProcessingResult, DomainError>> ProcessAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default);
}