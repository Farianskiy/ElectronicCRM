using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Core.Catalog.Products.GetReplacements;

namespace ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;

public sealed record CatalogAssistantResult(
    CatalogAssistantIntent Intent,
    string Answer,
    CatalogAssistantParsedRequest ParsedRequest,
    IReadOnlyCollection<CatalogProductListItemResult> Products,
    CatalogProductListItemResult? SourceProduct,
    IReadOnlyCollection<CatalogProductReplacementItemResult> Replacements);