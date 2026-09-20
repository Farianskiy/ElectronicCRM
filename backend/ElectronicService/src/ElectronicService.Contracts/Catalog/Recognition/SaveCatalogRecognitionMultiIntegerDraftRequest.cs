using System.ComponentModel.DataAnnotations;

namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record SaveCatalogRecognitionMultiIntegerDraftPartRequest(
    string? Literal,
    Guid? CharacteristicDefinitionId);

public sealed record SaveCatalogRecognitionMultiIntegerDraftRequest(
    Guid ManufacturerId,
    Guid ProductTypeId,
    [Required] IReadOnlyList<Guid> CharacteristicDefinitionIds,
    [Required] IReadOnlyList<string> ProductNames,
    [Required] string GeneratorVersion,
    [Required] IReadOnlyList<SaveCatalogRecognitionMultiIntegerDraftPartRequest> Parts);

public sealed record SaveCatalogRecognitionMultiIntegerDraftResponse(Guid Id);