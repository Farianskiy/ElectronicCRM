namespace ElectronicService.Core.Catalog.ImportBatches.RequestCatalogImportChanges;

public sealed record RequestCatalogImportChangesCommand(
    Guid BatchId,
    Guid CurrentUserId,
    string? Comment);