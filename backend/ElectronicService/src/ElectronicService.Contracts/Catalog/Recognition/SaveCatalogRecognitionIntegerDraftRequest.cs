using System.ComponentModel.DataAnnotations;

namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record SaveCatalogRecognitionIntegerDraftRequest(
    Guid ManufacturerId,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    [Required(AllowEmptyStrings = true), StringLength(2000)] string Prefix,
        [Required] IReadOnlyList<string> Suffixes,
    [Required, StringLength(100)] string GeneratorVersion);

public sealed record SaveCatalogRecognitionIntegerDraftResponse(Guid DraftId);