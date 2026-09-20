namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerAlternativesPattern(
    string Prefix,
    IReadOnlyList<string> Suffixes);

public sealed record CatalogRecognitionIntegerAlternativesProposal(
    CatalogRecognitionIntegerAlternativesPattern Pattern,
    int MatchedNameCount,
    int DistinctValueCount,
    IReadOnlyList<Guid> SupportingExampleIds,
    IReadOnlyList<Guid> ConflictingExampleIds)
{
    public bool PassedExamples => MatchedNameCount >= 2 && DistinctValueCount >= 2 && SupportingExampleIds.Count > 0 && ConflictingExampleIds.Count == 0;
}

public sealed record CatalogRecognitionIntegerAlternativesProposalSet(
    CatalogRecognitionTrainingScope Scope,
    string GeneratorVersion,
    IReadOnlyList<CatalogRecognitionIntegerAlternativesProposal> Proposals,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues);