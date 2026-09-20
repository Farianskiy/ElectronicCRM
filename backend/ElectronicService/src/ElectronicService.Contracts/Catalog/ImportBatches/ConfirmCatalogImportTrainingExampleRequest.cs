using System.ComponentModel.DataAnnotations;

namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record ConfirmCatalogImportTrainingExampleRequest(
    [Required, StringLength(2000)] string ProductName,
    Guid ManufacturerId,
    Guid ProductTypeId,
    [Required, StringLength(2000)] string NormalizedValue,
    [Required, StringLength(2000)] string RawValue,
    [Range(0, 1999)] int SpanStart,
    [Range(1, 2000)] int SpanLength);

public sealed record ConfirmCatalogImportTrainingExampleResponse(Guid ExampleId);