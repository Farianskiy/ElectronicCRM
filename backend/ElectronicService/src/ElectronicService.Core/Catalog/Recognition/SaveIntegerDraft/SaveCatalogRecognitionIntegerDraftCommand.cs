namespace ElectronicService.Core.Catalog.Recognition.SaveIntegerDraft;

public sealed record SaveCatalogRecognitionIntegerDraftCommand(
    Guid ManufacturerId,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    string Prefix,
    IReadOnlyList<string> Suffixes,
    string GeneratorVersion);