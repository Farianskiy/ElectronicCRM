namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerPattern(
    string Prefix,
    string Suffix);

public sealed record CatalogRecognitionIntegerCapture(
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength);

public sealed record CatalogRecognitionIntegerPatternProposal(
    CatalogRecognitionIntegerPattern Pattern,
    int MatchedNameCount,
    int DistinctValueCount,
    IReadOnlyList<Guid> SupportingExampleIds,
    IReadOnlyList<Guid> ConflictingExampleIds)
{
    public bool PassedExamples => MatchedNameCount >= 2 && DistinctValueCount >= 2 && SupportingExampleIds.Count > 0 && ConflictingExampleIds.Count == 0;
}

public sealed record CatalogRecognitionIntegerPatternProposalSet(
    CatalogRecognitionTrainingScope Scope,
    string GeneratorVersion,
    IReadOnlyList<CatalogRecognitionIntegerPatternProposal> Proposals,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues);