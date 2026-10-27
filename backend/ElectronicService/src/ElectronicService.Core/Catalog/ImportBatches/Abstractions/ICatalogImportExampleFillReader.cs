namespace ElectronicService.Core.Catalog.ImportBatches.Abstractions;

public interface ICatalogImportExampleFillReader
{
    Task<IReadOnlyCollection<CatalogImportExampleFillSample>> GetActiveSamplesAsync(
        Guid batchId,
        IReadOnlyCollection<Guid>? sourceRowIds = null,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogImportExampleFillSample(
    Guid ExampleId,
    Guid SourceRowId,
    Guid ManufacturerId,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string ProductName,
    string RawValue,
    string NormalizedValue);
