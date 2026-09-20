using ElectronicService.Core.Catalog.Recognition.Abstractions;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionMultiIntegerDraftRecheckService
{
    private readonly ICatalogRecognitionMultiIntegerDraftReader _draftReader;
    private readonly CatalogRecognitionMultiIntegerProposalService _proposalService;

    public CatalogRecognitionMultiIntegerDraftRecheckService(
        ICatalogRecognitionMultiIntegerDraftReader draftReader,
        CatalogRecognitionMultiIntegerProposalService proposalService)
    {
        _draftReader = draftReader;
        _proposalService = proposalService;
    }

    public async Task<CatalogRecognitionMultiIntegerDraftRecheckResult?> RecheckAsync(
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _draftReader.GetSnapshotAsync(draftId, cancellationToken).ConfigureAwait(false);

        if (draft is null)
        {
            return null;
        }

        var issues = new List<CatalogRecognitionTrainingIssue>();

        if (!string.Equals(draft.GeneratorVersion, CatalogRecognitionMultiIntegerProposalGenerator.Version, StringComparison.Ordinal))
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                "GeneratorVersionChanged",
                "Версия генератора изменилась. Требуется получить и проверить новое предложение.",
                []));
        }

        if (!CatalogRecognitionMultiIntegerPatternValidator.IsValid(draft.Pattern))
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                "InvalidStoredPattern",
                "Сохранённый шаблон имеет некорректную структуру.",
                []));
        }

        if (issues.Count > 0)
        {
            return CreateFailedResult(draft.Id, issues);
        }

        var characteristicIds = draft.Pattern.Parts
            .Where(part => part.CharacteristicDefinitionId.HasValue)
            .Select(part => part.CharacteristicDefinitionId.GetValueOrDefault())
            .ToArray();

        var preview = await _proposalService.PreviewAsync(
            draft.ManufacturerId,
            draft.ProductTypeId,
            characteristicIds,
            draft.ProductNames,
            cancellationToken).ConfigureAwait(false);

        if (preview.IsFailure)
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                preview.Error.Code,
                preview.Error.Message,
                []));

            return CreateFailedResult(draft.Id, issues);
        }

        var generated = preview.Value;

        if (generated.Issues.Count > 0)
        {
            return CreateFailedResult(draft.Id, generated.Issues);
        }

        if (generated.CheckedExampleIds.Count == 0)
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                "NoCheckedExamples",
                "Не получен полный состав проверенных подтверждений.",
                []));

            return CreateFailedResult(draft.Id, issues);
        }

        var evaluation = generated.Proposals.SingleOrDefault(proposal =>
            proposal.Pattern.Parts.SequenceEqual(draft.Pattern.Parts));

        if (evaluation is null)
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                "StoredPatternNotReproduced",
                "Сохранённая структура больше не воспроизводится по действующим примерам. Другой шаблон вместо неё не подставляется.",
                []));
        }
        else if (!evaluation.PassedExamples)
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                "StoredPatternFailed",
                "Сохранённый шаблон не проходит проверку: недостаточно разных поддерживающих значений или есть конфликтующая разметка.",
                evaluation.ConflictingExampleIds));
        }

        var storedIds = draft.CheckedExampleIds.ToHashSet();
        var currentIds = generated.CheckedExampleIds.ToHashSet();

        var addedIds = currentIds.Except(storedIds).OrderBy(id => id).ToArray();
        var missingIds = storedIds.Except(currentIds).OrderBy(id => id).ToArray();

        return new CatalogRecognitionMultiIntegerDraftRecheckResult(
            draft.Id,
            DateTime.UtcNow,
            CatalogRecognitionMultiIntegerProposalGenerator.Version,
            evaluation is not null && evaluation.PassedExamples && issues.Count == 0,
            true,
            storedIds.SetEquals(currentIds),
            addedIds,
            missingIds,
            evaluation,
            issues);
    }

    private static CatalogRecognitionMultiIntegerDraftRecheckResult CreateFailedResult(
        Guid draftId,
        IReadOnlyList<CatalogRecognitionTrainingIssue> issues)
    {
        return new CatalogRecognitionMultiIntegerDraftRecheckResult(
            draftId,
            DateTime.UtcNow,
            CatalogRecognitionMultiIntegerProposalGenerator.Version,
            false,
            false,
            null,
            [],
            [],
            null,
            issues);
    }
}