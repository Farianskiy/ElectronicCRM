namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record UpdateCatalogImportMappingRequest(
    Guid ProductTypeId,
    IReadOnlyCollection<UpdateCatalogImportColumnMappingRequest>? Columns);

public sealed record UpdateCatalogImportColumnMappingRequest(
    Guid ColumnId,
    string? TargetKind,
    Guid? CharacteristicDefinitionId);