using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportProductTypeSuggestionShadowService
{
    Task<CatalogImportProductTypeSuggestionShadowResult> AnalyzeAsync(
        CatalogImportWorkbookAnalysis analysis,
        ProductType? selectedProductType,
        CancellationToken cancellationToken = default);
}