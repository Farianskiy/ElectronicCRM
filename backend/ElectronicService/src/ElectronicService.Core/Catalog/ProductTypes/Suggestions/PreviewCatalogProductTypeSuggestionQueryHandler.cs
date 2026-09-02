namespace ElectronicService.Core.Catalog.ProductTypes.Suggestions;

public sealed class PreviewCatalogProductTypeSuggestionQueryHandler
{
    private readonly ICatalogProductTypeSuggestionService _suggestionService;

    public PreviewCatalogProductTypeSuggestionQueryHandler(
        ICatalogProductTypeSuggestionService suggestionService)
    {
        ArgumentNullException.ThrowIfNull(suggestionService);

        _suggestionService = suggestionService;
    }

    public Task<CatalogProductTypeSuggestionResult> Handle(
        PreviewCatalogProductTypeSuggestionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.ProductName);

        return _suggestionService.SuggestAsync(
            query.ProductName,
            cancellationToken);
    }
}