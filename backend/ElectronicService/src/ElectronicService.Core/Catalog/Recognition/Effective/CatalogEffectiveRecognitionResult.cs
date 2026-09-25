using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.Core.Catalog.Recognition.Effective;

public sealed record CatalogEffectiveRecognitionResult(
    CatalogProductNameRecognitionResult Recognition,
    bool HasCompleteScope,
    CatalogRecognitionRuleSetState? ActiveRuleSet,
    IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult> RecognitionProfiles);
