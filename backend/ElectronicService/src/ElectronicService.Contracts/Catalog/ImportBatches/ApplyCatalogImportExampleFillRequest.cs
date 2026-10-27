namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record ApplyCatalogImportExampleFillRequest(
    uint ExpectedVersion,
    IReadOnlyCollection<Guid>? ExampleRowIds,
    IReadOnlyCollection<Guid>? TargetRowIds);
