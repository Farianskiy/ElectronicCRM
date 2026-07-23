using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportWorkbookAnalyzer
{
    Result<
        CatalogImportWorkbookAnalysis,
        DomainError> Analyze(
            Guid batchId,
            ReadOnlyMemory<byte> workbookContent,
            ProductType? productType,
            IReadOnlyCollection<
                CharacteristicDefinition>
                characteristicDefinitions,
            CancellationToken cancellationToken =
                default);
}