namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionLiteralProposal(
    string Literal,
    string NormalizedValue,
    int MatchedNameCount,
    IReadOnlyList<Guid> SupportingExampleIds,
    IReadOnlyList<Guid> ConflictingExampleIds)
{
    public bool PassedExamples => MatchedNameCount > 0 && SupportingExampleIds.Count > 0 && ConflictingExampleIds.Count == 0;
}

public sealed record CatalogRecognitionLiteralProposalSet(
    CatalogRecognitionTrainingScope Scope,
    string GeneratorVersion,
    IReadOnlyList<CatalogRecognitionLiteralProposal> Proposals,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues);