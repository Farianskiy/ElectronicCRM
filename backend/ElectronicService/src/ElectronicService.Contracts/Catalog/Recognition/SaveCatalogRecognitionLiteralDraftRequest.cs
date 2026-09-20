using System.ComponentModel.DataAnnotations;

namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record SaveCatalogRecognitionLiteralDraftRequest(
    Guid ManufacturerId,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    [Required, StringLength(2000)] string Literal,
    [Required, StringLength(2000)] string NormalizedValue,
    [Required, StringLength(100)] string GeneratorVersion);

public sealed record SaveCatalogRecognitionLiteralDraftResponse(Guid DraftId);