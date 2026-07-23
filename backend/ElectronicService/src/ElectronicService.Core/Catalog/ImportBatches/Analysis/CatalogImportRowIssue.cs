namespace ElectronicService.Core.Catalog
    .ImportBatches.Analysis;

public sealed record CatalogImportRowIssue(
    string Code,
    string Message,
    string? Field,
    int? SourceColumnNumber);