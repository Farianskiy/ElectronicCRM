namespace ElectronicService.Core.Catalog.ProductTypes.Suggestions;

public interface ICatalogProductTypeSuggestionService
{
    Task<CatalogProductTypeSuggestionIndex> LoadIndexAsync(
        CancellationToken cancellationToken = default);

    Task<CatalogProductTypeSuggestionResult> SuggestAsync(
        string productName,
        CancellationToken cancellationToken = default);
}