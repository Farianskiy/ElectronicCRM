using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleValueCandidate(
    CatalogRecognitionRuleKind Kind,
    Guid DraftId,
    Guid CharacteristicDefinitionId,
    string NormalizedValue,
    string RawValue,
    int SpanStart,
    int SpanLength);

public sealed record CatalogRecognitionResolvedCharacteristic(
    Guid CharacteristicDefinitionId,
    string? ProposedValue,
    bool HasConflict,
    IReadOnlyList<string> AlternativeValues,
    IReadOnlyList<CatalogRecognitionRuleValueCandidate> Sources);

public sealed record CatalogRecognitionRuleSetValueResolution(
    IReadOnlyList<CatalogRecognitionResolvedCharacteristic> Characteristics)
{
    public bool HasConflicts => Characteristics.Any(item => item.HasConflict);
}