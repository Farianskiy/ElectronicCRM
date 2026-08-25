namespace ElectronicService.Core.Catalog.Recognition.Learning;

public sealed record CatalogRecognitionCandidateSuggestionPromotionResult(
    int ScannedCandidateCount,
    int EligibleCandidateCount,
    int CreatedSuggestionCount,
    int AttachedExistingSuggestionCount,
    int DeferredCandidateCount);