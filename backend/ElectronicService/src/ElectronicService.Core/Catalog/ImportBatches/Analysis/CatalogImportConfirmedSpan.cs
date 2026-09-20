namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportConfirmedSpan(string ProductName, int Start, int Length);