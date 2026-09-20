using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionRuleSetTrainingCheckService
{
    private readonly ICatalogRecognitionRuleSetExecutionReader _executionReader;
    private readonly CatalogRecognitionLiteralDraftRecheckService _literalService;
    private readonly CatalogRecognitionIntegerDraftRecheckService _numericService;
    private readonly CatalogRecognitionMultiIntegerDraftRecheckService _multipleService;

    public CatalogRecognitionRuleSetTrainingCheckService(
        ICatalogRecognitionRuleSetExecutionReader executionReader,
        CatalogRecognitionLiteralDraftRecheckService literalService,
        CatalogRecognitionIntegerDraftRecheckService numericService,
        CatalogRecognitionMultiIntegerDraftRecheckService multipleService)
    {
        _executionReader = executionReader;
        _literalService = literalService;
        _numericService = numericService;
        _multipleService = multipleService;
    }

    public async Task<Result<CatalogRecognitionRuleSetTrainingCheckResult, DomainError>>
        CheckAsync(
            Guid versionId,
            CancellationToken cancellationToken = default)
    {
        if (versionId == Guid.Empty)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите версию правил.");
        }

        var loaded = await _executionReader.ReadAsync(
            versionId,
            cancellationToken).ConfigureAwait(false);

        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        var snapshot = loaded.Value;
        var items = new List<CatalogRecognitionRuleSetTrainingCheckItem>();

        foreach (var draftId in snapshot.LiteralRules.Select(rule => rule.DraftId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _literalService.RecheckAsync(
                draftId,
                cancellationToken).ConfigureAwait(false);

            var passed = result is not null &&
                result.GeneratorVersionMatches &&
                result.PassedCurrentExamples &&
                result.EvidenceUnchanged;

            items.Add(CreateItem(
                draftId,
                "Literal",
                passed));
        }

        foreach (var draftId in snapshot.NumericRules.Select(rule => rule.DraftId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _numericService.RecheckAsync(
                draftId,
                cancellationToken).ConfigureAwait(false);

            var passed = result is not null &&
                result.GeneratorVersionMatches &&
                result.PassedCurrentExamples &&
                result.EvidenceUnchanged;

            items.Add(CreateItem(
                draftId,
                "NumericCapture",
                passed));
        }

        foreach (var rule in snapshot.MultiNumericRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _multipleService.RecheckAsync(
                rule.DraftId,
                cancellationToken).ConfigureAwait(false);

            var passed = result is not null &&
                result.Passed &&
                result.SelectionComplete &&
                result.EvidenceUnchanged == true &&
                result.Issues.Count == 0 &&
                string.Equals(
                    rule.GeneratorVersion,
                    result.CurrentGeneratorVersion,
                    StringComparison.Ordinal);

            items.Add(CreateItem(
                rule.DraftId,
                "MultipleNumericCaptures",
                passed));
        }

        return new CatalogRecognitionRuleSetTrainingCheckResult(
            versionId,
            DateTime.UtcNow,
            items);
    }

    private static CatalogRecognitionRuleSetTrainingCheckItem CreateItem(
        Guid draftId,
        string ruleKind,
        bool passed)
    {
        var message = passed
            ? "Учебная проверка пройдена. Состав подтверждений не изменился."
            : "Черновик отсутствует, изменились учебные основания или текущая проверка не пройдена. Требуется перепроверить черновик и при необходимости подготовить новую версию.";

        return new CatalogRecognitionRuleSetTrainingCheckItem(
            draftId,
            ruleKind,
            passed,
            message);
    }
}