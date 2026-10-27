namespace ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;

public sealed record PreviewCatalogImportExampleFillQuery(
    Guid BatchId,
    Guid CurrentUserId,
    uint ExpectedVersion,
    IReadOnlyCollection<Guid>? ExampleRowIds,
    IReadOnlyCollection<Guid>? TargetRowIds);
