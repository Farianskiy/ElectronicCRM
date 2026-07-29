namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportMapping;

public sealed record GetCatalogImportMappingQuery(
    Guid BatchId,
    Guid CurrentUserId);