using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.Core.Catalog.Recognition.SaveMultiIntegerDraft;

public sealed record SaveCatalogRecognitionMultiIntegerDraftCommand(
    Guid ManufacturerId,
    Guid ProductTypeId,
    IReadOnlyList<Guid> CharacteristicDefinitionIds,
    IReadOnlyList<string> ProductNames,
    string GeneratorVersion,
    CatalogRecognitionMultiIntegerPattern Pattern);