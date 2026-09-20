namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerDraftSnapshot(
    Guid Id,
    CatalogRecognitionTrainingScope Scope,
    CatalogRecognitionIntegerAlternativesPattern Pattern,
    string GeneratorVersion,
    IReadOnlyList<Guid> CheckedExampleIds);

public sealed record CatalogRecognitionIntegerDraftRecheckResult(
    Guid DraftId,
    DateTime CheckedAtUtc,
    string CurrentGeneratorVersion,
    bool GeneratorVersionMatches,
    bool SelectionComplete,
    bool EvidenceUnchanged,
    int LoadedExampleCount,
    IReadOnlyList<Guid> AddedExampleIds,
    IReadOnlyList<Guid> MissingExampleIds,
    CatalogRecognitionIntegerAlternativesProposal? Evaluation,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues)
{
    public bool PassedCurrentExamples => GeneratorVersionMatches && SelectionComplete && Issues.Count == 0 && Evaluation is { PassedExamples: true };
}