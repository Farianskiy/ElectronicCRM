using System.Data;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class DictionaryEvaluationReports(ElectronicDbContext db, RecognitionEvaluationAccess access, DictionaryEvaluationCapture capture,
    RecognitionComparisonEvaluator evaluator, IOptions<RecognitionEvaluationOptions> options) : IDictionaryEvaluationReports
{
    private async Task<Result<T, DomainError>> TimedAsync<T>(Func<CancellationToken, Task<Result<T, DomainError>>> action, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try { return await action(deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return new DomainError("evaluation.timeout", "Оценка превысила допустимое время. Частичный результат не сохранён."); }
    }

    public Task<Result<Guid, DomainError>> CreateAsync(ApproveCatalogAssistantDictionarySuggestionCommand command, CancellationToken ct) => TimedAsync<Guid>(async token =>
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token).ConfigureAwait(false);
        var auth = await access.AuthorizeAsync(token).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        var snapshot = await capture.CaptureAsync(command, auth.Value, token).ConfigureAwait(false);
        if (snapshot.IsFailure) return snapshot.Error;
        var evaluated = await evaluator.EvaluateAsync(snapshot.Value.Input, token).ConfigureAwait(false);
        if (evaluated.IsFailure) return evaluated.Error;
        var value = snapshot.Value with { Result = evaluated.Value };
        var created = CatalogDictionaryEvaluationReport.Create(value.SuggestionId, value.CandidateId, auth.Value, EvaluationJson.Serialize(value));
        if (created.IsFailure) return created.Error;
        db.Set<CatalogDictionaryEvaluationReport>().Add(created.Value);
        await db.SaveChangesAsync(token).ConfigureAwait(false);
        await transaction.CommitAsync(token).ConfigureAwait(false);
        return created.Value.Id;
    }, ct);

    private async Task<Result<DictionaryEvaluationSnapshot, DomainError>> LoadAsync(Guid id, Guid owner, CancellationToken ct)
    {
        var report = await db.Set<CatalogDictionaryEvaluationReport>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == owner, ct).ConfigureAwait(false);
        if (report is null) return new DomainError("training.not_found", "Отчёт не найден или недоступен.");
        try
        {
            var snapshot = JsonSerializer.Deserialize<DictionaryEvaluationSnapshot>(report.SnapshotJson, EvaluationJson.Options);
            if (snapshot is null || snapshot.FormatVersion != 1 || !string.Equals(snapshot.EvaluatorVersion, RecognitionComparisonEvaluator.Version, StringComparison.Ordinal))
                return new DomainError("evaluation.unsupported_format", "Формат или исполнитель словарного отчёта не поддерживается.");
            if (snapshot.Input is null || snapshot.Input.CandidateTerms is null || snapshot.Decision is null || snapshot.Result is null ||
                snapshot.SuggestionId != report.SuggestionId || snapshot.CandidateId != report.CandidateId ||
                !string.Equals(snapshot.InputFingerprint, EvaluationJson.Fingerprint(snapshot.Input), StringComparison.Ordinal))
                return new DomainError("evaluation.invalid_snapshot", "Снимок словарной оценки повреждён.");
            return snapshot;
        }
        catch (JsonException) { return new DomainError("evaluation.invalid_snapshot", "Снимок словарной оценки не удалось прочитать."); }
    }

    private async Task<EvaluationReadiness> ReadinessAsync(DictionaryEvaluationSnapshot saved, ApproveCatalogAssistantDictionarySuggestionCommand command, Guid owner, CancellationToken ct)
    {
        var fresh = await capture.CaptureAsync(command, owner, ct).ConfigureAwait(false);
        if (fresh.IsFailure) return new("Stale", [new("evaluation.stale", fresh.Error.Message)]);
        var current = fresh.Value;
        if (saved.SuggestionId != current.SuggestionId || saved.CandidateId != current.CandidateId || saved.EvidenceRevision != current.EvidenceRevision ||
            saved.Decision != current.Decision || !string.Equals(saved.ReviewComment, current.ReviewComment, StringComparison.Ordinal) ||
            !string.Equals(saved.InputFingerprint, current.InputFingerprint, StringComparison.Ordinal) ||
            !string.Equals(saved.EvidenceFingerprint, current.EvidenceFingerprint, StringComparison.Ordinal))
            return new("Stale", [new("evaluation.stale", "Решение, основания, разметка или конфигурация изменились. Создайте новую оценку.")]);
        if (!saved.SufficientEvidence || !current.SufficientEvidence)
            return new("Failed", [new("evaluation.insufficient_evidence", "Учебные основания недостаточны или неполны.")]);
        return saved.Result.Readiness;
    }

    public async Task<UnitResult<DomainError>> ValidateAsync(ApproveCatalogAssistantDictionarySuggestionCommand command, CatalogDictionarySuggestionApprovalDecision decision, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) return UnitResult.Failure(new DomainError("evaluation.invalid_operation", "Одобрение требует транзакции."));
        if (!command.Confirmed || command.EvaluationReportId is not Guid reportId || reportId == Guid.Empty)
            return UnitResult.Failure(new DomainError("evaluation.report_required", "Оцените влияние и явно подтвердите одобрение проверенного решения."));
        var check = await TimedAsync<bool>(async token =>
        {
            var auth = await access.AuthorizeAsync(token).ConfigureAwait(false);
            if (auth.IsFailure) return auth.Error;
            var loaded = await LoadAsync(reportId, auth.Value, token).ConfigureAwait(false);
            if (loaded.IsFailure) return loaded.Error;
            if (DictionaryEvaluationDecision.From(decision) != loaded.Value.Decision)
                return new DomainError("evaluation.stale", "Поля окончательного решения изменились. Создайте новую оценку.");
            var readiness = await ReadinessAsync(loaded.Value, command, auth.Value, token).ConfigureAwait(false);
            if (!string.Equals(readiness.State, "Ready", StringComparison.Ordinal))
                return new DomainError(string.Equals(readiness.State, "Stale", StringComparison.Ordinal) ? "evaluation.stale" : "evaluation.not_ready", string.Join(" ", readiness.Reasons.Select(x => x.Message)));
            return true;
        }, ct).ConfigureAwait(false);
        return check.IsSuccess ? UnitResult.Success<DomainError>() : UnitResult.Failure(check.Error);
    }

    public Task<Result<DictionaryEvaluationPage, DomainError>> ReadAsync(Guid id, int page, bool replay, CancellationToken ct) => TimedAsync<DictionaryEvaluationPage>(async token =>
    {
        if (page is < 1 or > 10000) return new DomainError("training.invalid_request", "Некорректная страница.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token).ConfigureAwait(false);
        var auth = await access.AuthorizeAsync(token).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        var loaded = await LoadAsync(id, auth.Value, token).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.Error;
        var saved = loaded.Value;
        var result = saved.Result;
        EvaluationReadiness readiness;
        if (replay)
        {
            var repeated = await evaluator.EvaluateAsync(saved.Input, token).ConfigureAwait(false);
            if (repeated.IsFailure) return repeated.Error;
            result = repeated.Value;
            readiness = new("Historical", [new("evaluation.historical_replay", "Повтор исторического снимка, включая прежние источники. Это не разрешение одобрить предложение.")]);
        }
        else readiness = await ReadinessAsync(saved, new(saved.SuggestionId, saved.Decision.Phrase, saved.Decision.Kind.ToString(), saved.Decision.TargetCode,
            saved.Decision.TargetValue, saved.ProductTypeCode, saved.Decision.Priority, saved.ReviewComment, saved.EvidenceRevision), auth.Value, token).ConfigureAwait(false);
        var used = await db.CatalogAssistantDictionarySuggestions.AsNoTracking().AnyAsync(x => x.Id == saved.SuggestionId && x.EvaluationReportId == id, token).ConfigureAwait(false);
        var input = saved.Input;
        var view = new EvaluationPage(id, input.ManufacturerId, input.ProductTypeId, input.CurrentState.ActiveVersionId, input.CurrentState.ActiveVersionId ?? Guid.Empty,
            input.CurrentState.SequenceNumber, replay, input.Policy, readiness, result.Control, result.TrainingDiagnostic, result.Characteristics,
            result.InputExamples, result.DuplicateExamples, result.LabelConflicts, result.OverlapUnits, saved.SufficientEvidence, page, result.Rows.Count,
            result.Rows.Skip((page - 1) * 25).Take(25).ToArray()) { ManufacturerName = input.ManufacturerName, ProductTypeName = input.ProductTypeName };
        return new DictionaryEvaluationPage(view, saved.SuggestionId, saved.Decision, used);
    }, ct);
}
