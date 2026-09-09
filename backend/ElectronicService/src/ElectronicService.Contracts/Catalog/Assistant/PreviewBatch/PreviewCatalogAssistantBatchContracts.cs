using ElectronicService.Contracts.Catalog.Products;

namespace ElectronicService.Contracts.Catalog.Assistant.PreviewBatch;

public sealed class PreviewCatalogAssistantBatchRequest
{
    public string Message { get; init; } = string.Empty;

    public bool OnlyInStock { get; init; }

    public int MatchesPerLine { get; init; } = 5;
}

public sealed record CatalogAssistantBatchLineResponse(int LineNumber, string SourceText, string SearchText, decimal? Quantity, string Status, string Message, string? Manufacturer, IReadOnlyCollection<CatalogAssistantCharacteristicResponse> Characteristics, IReadOnlyCollection<ProductListItemResponse> Products);

public sealed record PreviewCatalogAssistantBatchResponse(string? CommonText, int TotalLines, int MatchedLines, int RequiresAttentionLines, IReadOnlyCollection<CatalogAssistantBatchLineResponse> Lines);