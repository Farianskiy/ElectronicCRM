namespace ElectronicService.Core.Catalog.ImportBatches.DownloadCatalogImportFile;

public sealed record DownloadCatalogImportFileResult(
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> Content);