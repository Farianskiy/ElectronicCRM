using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportRecognitionEnrichmentService
{
    Task<Result<CatalogImportRecognitionEnrichmentResult, DomainError>> EnrichAsync(
        CatalogImportWorkbookAnalysis analysis,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions,
        CancellationToken cancellationToken = default);
}