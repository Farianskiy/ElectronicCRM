using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportMapping;

public sealed record UpdateCatalogImportMappingCommand(
    Guid BatchId,
    Guid CurrentUserId,
    Guid ProductTypeId,
    IReadOnlyCollection<UpdateCatalogImportColumnMapping> Columns);

public sealed record UpdateCatalogImportColumnMapping(
    Guid ColumnId,
    CatalogImportColumnTargetKind TargetKind,
    Guid? CharacteristicDefinitionId);