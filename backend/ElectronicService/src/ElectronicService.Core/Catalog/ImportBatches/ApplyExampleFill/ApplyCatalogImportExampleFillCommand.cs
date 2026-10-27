namespace ElectronicService.Core.Catalog.ImportBatches.ApplyExampleFill;

public sealed record ApplyCatalogImportExampleFillCommand(
    Guid BatchId,
    Guid CurrentUserId,
    uint ExpectedVersion,
    IReadOnlyCollection<Guid>? ExampleRowIds,
    IReadOnlyCollection<Guid>? TargetRowIds);
