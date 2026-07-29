namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportMappingResponse(
    Guid BatchId,
    string Status,
    Guid? ProductTypeId,
    IReadOnlyCollection<CatalogImportColumnMappingResponse> Columns,
    uint Version,
    bool CanEdit);

public sealed record CatalogImportColumnMappingResponse(
    Guid ColumnId,
    int SourceColumnNumber,
    string SourceHeader,
    string TargetKind,
    Guid? CharacteristicDefinitionId,
    decimal Confidence,
    bool IsConfirmed);