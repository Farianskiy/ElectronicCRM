namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionLiteralDraftRecheckResult(
    Guid DraftId,
    DateTime CheckedAtUtc,
    string CurrentGeneratorVersion,
    bool GeneratorVersionMatches,
    bool SelectionComplete,
    bool EvidenceUnchanged,
    int CurrentExampleCount,
    IReadOnlyList<Guid> AddedExampleIds,
    IReadOnlyList<Guid> MissingExampleIds,
    CatalogRecognitionLiteralProposal? Proposal,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues)
{
    public bool PassedCurrentExamples => SelectionComplete && Issues.Count == 0 && Proposal is { PassedExamples: true };
}