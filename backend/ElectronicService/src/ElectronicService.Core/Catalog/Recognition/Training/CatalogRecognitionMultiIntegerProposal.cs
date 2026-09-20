namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerFieldCoverage(
    Guid CharacteristicDefinitionId,
    int DistinctValueCount);

public sealed record CatalogRecognitionMultiIntegerProposal(
    CatalogRecognitionMultiIntegerPattern Pattern,
    int MatchedNameCount,
    int SupportingNameCount,
    IReadOnlyList<CatalogRecognitionMultiIntegerFieldCoverage> Fields,
    IReadOnlyList<Guid> SupportingExampleIds,
    IReadOnlyList<Guid> ConflictingExampleIds)
{
    public bool PassedExamples => SupportingNameCount >= 2
        && ConflictingExampleIds.Count == 0
        && Fields.Count >= 2
        && Fields.All(coverage => coverage.DistinctValueCount >= 2);
}

public sealed record CatalogRecognitionMultiIntegerProposalSet(
    Guid ManufacturerId,
    Guid ProductTypeId,
    string GeneratorVersion,
    IReadOnlyList<CatalogRecognitionMultiIntegerProposal> Proposals,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues)
{
    public IReadOnlyList<Guid> CheckedExampleIds { get; init; } = [];
}