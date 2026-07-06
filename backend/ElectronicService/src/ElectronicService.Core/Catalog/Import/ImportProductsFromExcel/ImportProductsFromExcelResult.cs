namespace ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;

public sealed record ImportProductsFromExcelResult(
    int TotalRows,
    int ImportedRows,
    int SkippedRows,
    IReadOnlyCollection<string> Errors);