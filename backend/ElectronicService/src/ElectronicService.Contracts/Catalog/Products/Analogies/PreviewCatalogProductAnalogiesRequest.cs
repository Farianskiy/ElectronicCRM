namespace ElectronicService.Contracts.Catalog.Products.Analogies;

public sealed record PreviewCatalogProductAnalogiesRequest(
    string ProductName,
    string? Article,
    Guid ManufacturerId,
    Guid? ProductTypeId,
    IReadOnlyDictionary<string, string>? KnownCharacteristics,
    int MaximumResults = 20,
    Guid? ImportBatchId = null,
    Guid? ImportRowId = null);
