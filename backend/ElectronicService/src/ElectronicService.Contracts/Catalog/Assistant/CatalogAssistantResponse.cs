using ElectronicService.Contracts.Catalog.Products;
using ElectronicService.Contracts.Catalog.Products.Replacements;

namespace ElectronicService.Contracts.Catalog.Assistant;

public sealed record CatalogAssistantResponse(
    string Intent,
    bool NeedsClarification,
    string Answer,
    CatalogAssistantParsedRequestResponse ParsedRequest,
    IReadOnlyCollection<ProductListItemResponse> Products,
    ProductListItemResponse? SourceProduct,
    IReadOnlyCollection<ProductReplacementItemResponse> Replacements);