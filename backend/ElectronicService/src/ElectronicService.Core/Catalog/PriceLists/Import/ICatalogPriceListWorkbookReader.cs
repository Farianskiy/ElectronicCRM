using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.Import;

public interface ICatalogPriceListWorkbookReader
{
    Task<Result<CatalogPriceListWorkbookReadSummary, DomainError>> ReadAsync(
        string originalFileName,
        ReadOnlyMemory<byte> fileContent,
        Func<
            CatalogPriceListSourceRow,
            CancellationToken,
            Task<UnitResult<DomainError>>> consumeRowAsync,
        Func<
            CatalogPriceListWorkbookReadProgress,
            CancellationToken,
            Task<UnitResult<DomainError>>> reportProgressAsync,
        CancellationToken cancellationToken = default);
}