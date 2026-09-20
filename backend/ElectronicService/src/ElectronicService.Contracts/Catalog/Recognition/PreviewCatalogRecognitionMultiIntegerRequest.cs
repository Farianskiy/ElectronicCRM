using System.ComponentModel.DataAnnotations;

namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record PreviewCatalogRecognitionMultiIntegerRequest(
    Guid ManufacturerId,
    Guid ProductTypeId,
    [Required] IReadOnlyList<Guid> CharacteristicDefinitionIds,
    [Required] IReadOnlyList<string> ProductNames);