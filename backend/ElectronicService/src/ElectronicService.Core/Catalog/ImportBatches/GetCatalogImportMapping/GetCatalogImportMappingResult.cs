using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportMapping;

public sealed record GetCatalogImportMappingResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    Guid? ProductTypeId,
    IReadOnlyCollection<CatalogImportColumnMappingResult> Columns,
    uint Version,
    bool CanEdit);

public sealed record CatalogImportColumnMappingResult(
    Guid ColumnId,
    int SourceColumnNumber,
    string SourceHeader,
    CatalogImportColumnTargetKind TargetKind,
    Guid? CharacteristicDefinitionId,
    decimal Confidence,
    bool IsConfirmed);