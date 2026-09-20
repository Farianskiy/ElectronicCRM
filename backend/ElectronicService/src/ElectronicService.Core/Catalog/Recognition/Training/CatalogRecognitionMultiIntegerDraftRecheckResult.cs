namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerDraftSnapshot(
    Guid Id,
    Guid ManufacturerId,
    Guid ProductTypeId,
    string GeneratorVersion,
    CatalogRecognitionMultiIntegerPattern Pattern,
    IReadOnlyList<string> ProductNames,
    IReadOnlyList<Guid> CheckedExampleIds);

public sealed record CatalogRecognitionMultiIntegerDraftRecheckResult(
    Guid DraftId,
    DateTime CheckedAtUtc,
    string CurrentGeneratorVersion,
    bool Passed,
    bool SelectionComplete,
    bool? EvidenceUnchanged,
    IReadOnlyList<Guid> AddedExampleIds,
    IReadOnlyList<Guid> MissingExampleIds,
    CatalogRecognitionMultiIntegerProposal? Evaluation,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues);