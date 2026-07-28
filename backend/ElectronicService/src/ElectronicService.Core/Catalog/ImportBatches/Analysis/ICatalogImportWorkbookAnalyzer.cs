using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog
    .ImportBatches.Analysis;

public interface ICatalogImportWorkbookAnalyzer
{
    Result<CatalogImportWorkbookAnalysis, DomainError> Analyze(
    Guid batchId,
    ReadOnlyMemory<byte> workbookContent,
    ProductType? productType,
    IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions,
    IReadOnlyCollection<Manufacturer> manufacturers,
    IReadOnlyCollection<CatalogImportColumn> existingColumns,
    CancellationToken cancellationToken = default);
}