namespace ElectronicService.Core.Catalog.Recognition.SaveLiteralDraft;

public sealed record SaveCatalogRecognitionLiteralDraftCommand(
    Guid ManufacturerId,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    string Literal,
    string NormalizedValue,
    string GeneratorVersion);