namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetTrainingCheckItem(
    Guid DraftId,
    string RuleKind,
    bool Passed,
    string Message);

public sealed record CatalogRecognitionRuleSetTrainingCheckResult(
    Guid VersionId,
    DateTime CheckedAtUtc,
    IReadOnlyList<CatalogRecognitionRuleSetTrainingCheckItem> Items)
{
    public bool PassedTrainingChecks =>
        Items.Count > 0 && Items.All(item => item.Passed);
}