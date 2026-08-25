namespace ElectronicService.Core.Catalog.Recognition.Learning;

public sealed record CatalogRecognitionCandidateAggregationResult(
    int ScannedFeedbackCount,
    int CreatedCandidateCount,
    int UpdatedCandidateCount,
    int AddedEvidenceCount,
    int AlreadyProcessedFeedbackCount,
    int DeferredFeedbackCount);