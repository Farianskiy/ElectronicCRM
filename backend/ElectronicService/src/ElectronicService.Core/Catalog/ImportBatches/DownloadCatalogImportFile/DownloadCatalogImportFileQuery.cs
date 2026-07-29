namespace ElectronicService.Core.Catalog.ImportBatches.DownloadCatalogImportFile;

public sealed record DownloadCatalogImportFileQuery(
    Guid BatchId,
    Guid CurrentUserId);