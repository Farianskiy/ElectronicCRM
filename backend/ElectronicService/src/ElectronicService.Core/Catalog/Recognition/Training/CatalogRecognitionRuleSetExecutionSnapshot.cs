namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionLiteralExecutionRule(
    Guid DraftId,
    Guid CharacteristicDefinitionId,
    string GeneratorVersion,
    string Literal,
    string NormalizedValue);

public sealed record CatalogRecognitionNumericExecutionRule(
    Guid DraftId,
    Guid CharacteristicDefinitionId,
    string GeneratorVersion,
    CatalogRecognitionIntegerAlternativesPattern Pattern);

public sealed record CatalogRecognitionMultiNumericExecutionRule(
    Guid DraftId,
    string GeneratorVersion,
    CatalogRecognitionMultiIntegerPattern Pattern);

public sealed record CatalogRecognitionRuleSetExecutionSnapshot(
    Guid VersionId,
    Guid ManufacturerId,
    Guid ProductTypeId,
    int VersionNumber,
    IReadOnlyList<CatalogRecognitionLiteralExecutionRule> LiteralRules,
    IReadOnlyList<CatalogRecognitionNumericExecutionRule> NumericRules,
    IReadOnlyList<CatalogRecognitionMultiNumericExecutionRule> MultiNumericRules);