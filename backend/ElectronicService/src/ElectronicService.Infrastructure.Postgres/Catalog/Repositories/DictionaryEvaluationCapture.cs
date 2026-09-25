using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Core.Catalog.Recognition.Learning;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class DictionaryEvaluationCapture(ElectronicDbContext db, RecognitionEvaluationCapture capture,
    ICatalogProductTypeSchemaReader schemas, ICatalogDictionaryRepository dictionary)
{
    public async Task<Result<DictionaryEvaluationSnapshot, DomainError>> CaptureAsync(ApproveCatalogAssistantDictionarySuggestionCommand command, Guid userId, CancellationToken ct)
    {
        var suggestion = await db.CatalogAssistantDictionarySuggestions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == command.SuggestionId, ct).ConfigureAwait(false);
        if (suggestion is null) return new DomainError("training.not_found", "Предложение не найдено.");
        if (!suggestion.IsGeneratedFromRecognitionLearning || !suggestion.IsPending)
            return new DomainError("evaluation.stale", "Нужна новая оценка ожидающего решения предложения из обучения.");
        var candidate = await db.CatalogRecognitionCandidates.AsNoTracking().SingleOrDefaultAsync(x => x.SuggestionId == suggestion.Id, ct).ConfigureAwait(false);
        if (candidate is null || candidate.ManufacturerId is not Guid manufacturer || manufacturer == Guid.Empty || suggestion.ManufacturerId != manufacturer ||
            suggestion.ProductTypeId != candidate.ProductTypeId || command.EvidenceRevision != candidate.EvidenceRevision)
            return new DomainError("evaluation.stale", "Область или основания предложения изменились. Обновите основания и оценку.");
        if (!string.Equals(command.Kind, nameof(CatalogDictionaryTermKind.Characteristic), StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(command.ProductTypeCode))
            return new DomainError("evaluation.unsupported_decision", "Для предложения из обучения требуется характеристика и полная исходная область.");
        var schema = await schemas.GetByCodeAsync(command.ProductTypeCode.Trim(), ct).ConfigureAwait(false);
        if (schema is null || schema.ProductTypeId != candidate.ProductTypeId)
            return new DomainError("evaluation.unsupported_decision", "Изменение области предложения не поддерживается. Выберите исходный тип товара.");
        var code = (command.TargetCode ?? "").Trim().ToUpperInvariant().Replace("Ё", "Е", StringComparison.Ordinal).Replace(" ", "_", StringComparison.Ordinal).Replace("-", "_", StringComparison.Ordinal);
        var characteristic = schema.Characteristics.SingleOrDefault(x => string.Equals(x.Code, code, StringComparison.Ordinal));
        if (characteristic is null) return new DomainError("evaluation.unsupported_decision", "Характеристика отсутствует в схеме области.");
        var decision = CatalogDictionarySuggestionApprovalDecision.Create(command.Phrase, CatalogDictionaryTermKind.Characteristic, code, command.TargetValue,
            manufacturer, candidate.ProductTypeId, characteristic.DefinitionId, command.Priority);
        if (decision.IsFailure) return decision.Error;
        if (command.ReviewComment?.Length > CatalogAssistantDictionarySuggestion.ReviewCommentMaxLength)
            return new DomainError("evaluation.unsupported_decision", "Комментарий слишком длинный.");
        var normalized = DictionaryEvaluationDecision.From(decision.Value);
        var termResult = normalized.CreateTerm();
        if (termResult.IsFailure) return termResult.Error;
        var term = termResult.Value;
        if (await dictionary.ExistsAsync(term, ct).ConfigureAwait(false)) return new DomainError("evaluation.stale", "Такой термин уже существует. Повторный выпуск не требуется.");
        var captured = await capture.CaptureScopeAsync(manufacturer, candidate.ProductTypeId, userId, null, ct).ConfigureAwait(false);
        if (captured.IsFailure) return captured.Error;
        var evidenceQuery = db.CatalogRecognitionCandidateEvidenceEntries.AsNoTracking().Where(x => x.CandidateId == candidate.Id);
        var evidenceCount = await evidenceQuery.CountAsync(ct).ConfigureAwait(false);
        if (evidenceCount > 20000) return new DomainError("evaluation.selection_too_large", "Слишком много оснований для ограниченного снимка.");
        var evidence = await (from e in evidenceQuery join f in db.CatalogRecognitionFeedbackEntries.AsNoTracking() on e.FeedbackId equals f.Id
            orderby e.Id select new { e.Id, e.FeedbackId, f.ProductName, f.NormalizedProductName, f.ManufacturerId, f.ProductTypeId,
                f.CharacteristicDefinitionId, f.FeedbackType, f.Status, f.IsTrainingEligible, f.ExcludedAtUtc, f.FinalNormalizedValue }).ToArrayAsync(ct).ConfigureAwait(false);
        var allowed = evidence.Where(x => x.ExcludedAtUtc is null && x.Status == CatalogRecognitionFeedbackStatus.Finalized && x.IsTrainingEligible).ToArray();
        var complete = evidenceCount > 0 && evidenceCount == evidence.Length && evidence.All(x => x.ManufacturerId == manufacturer && x.ProductTypeId == candidate.ProductTypeId);
        var sufficient = complete && CatalogRecognitionCandidateSuggestionPolicy.HasSufficientEvidence(candidate) && allowed.Length == candidate.OccurrenceCount &&
            allowed.Count(x => x.FeedbackType == CatalogRecognitionFeedbackType.Corrected) == candidate.CorrectedCount &&
            allowed.Count(x => x.FeedbackType == CatalogRecognitionFeedbackType.Accepted) == candidate.AcceptedCount &&
            allowed.Count(x => x.FeedbackType == CatalogRecognitionFeedbackType.Rejected) == candidate.RejectedCount &&
            allowed.Select(x => x.NormalizedProductName).Distinct(StringComparer.Ordinal).Count() == candidate.DistinctProductCount;
        var input = captured.Value;
        var overlapNames = input.EvidenceNames.Concat(evidence.Select(x => CatalogRecognitionTextNormalizer.NormalizeText(x.ProductName)))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (overlapNames.Length > 20000 || input.Terms.Count >= 10000)
            return new DomainError("evaluation.selection_too_large", "Предлагаемая конфигурация превышает лимиты терминов или учебных имён. Отчёт не сохранён.");
        // Stable simulation identity/time are independent of the eventual persisted term identity/time.
        var simulated = new CatalogDictionaryTermResult(suggestion.Id, manufacturer, candidate.ProductTypeId, term.Phrase, term.NormalizedPhrase,
            term.Kind.ToString(), term.TargetCode, term.TargetValue, term.Priority, term.Status.ToString(), term.Source.ToString(),
            DateTime.UnixEpoch, DateTime.UnixEpoch, null, null, null, null, null, null, null);
        input = input with { CandidateTerms = input.Terms.Append(simulated).OrderBy(x => x.Id).ToArray(),
            EvidenceNames = overlapNames };
        return new DictionaryEvaluationSnapshot(1, RecognitionComparisonEvaluator.Version, suggestion.Id, candidate.Id, candidate.EvidenceRevision,
            normalized, command.ReviewComment?.Trim(), schema.ProductTypeCode, input, EvaluationJson.Fingerprint(input), EvaluationJson.Fingerprint(evidence),
            null!, sufficient);
    }
}
