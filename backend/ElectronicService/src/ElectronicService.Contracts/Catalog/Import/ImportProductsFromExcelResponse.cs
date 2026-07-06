namespace ElectronicService.Contracts.Catalog.Import;

public sealed record ImportProductsFromExcelResponse(
    int TotalRows,
    int ImportedRows,
    int SkippedRows,
    IReadOnlyCollection<string> Errors);