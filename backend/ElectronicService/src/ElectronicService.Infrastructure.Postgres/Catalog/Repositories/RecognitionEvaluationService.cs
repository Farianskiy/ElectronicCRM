using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using Microsoft.Extensions.Options;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class RecognitionEvaluationService(RecognitionEvaluationCapture capture, RecognitionComparisonEvaluator evaluator,
    CatalogRecognitionRuleSetTrainingCheckService training, IOptions<RecognitionEvaluationOptions> options)
{
    public async Task<Result<RecognitionEvaluationSnapshot, DomainError>> CreateAsync(Guid versionId, Guid userId, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try
        {
            var input = await capture.CaptureAsync(versionId, userId, deadline.Token).ConfigureAwait(false);
            if (input.IsFailure) return input.Error;
            var result = await evaluator.EvaluateAsync(input.Value, deadline.Token).ConfigureAwait(false);
            if (result.IsFailure) return result.Error;
            var checkedTraining = await training.CheckAsync(versionId, deadline.Token).ConfigureAwait(false);
            if (checkedTraining.IsFailure) return checkedTraining.Error;
            return new RecognitionEvaluationSnapshot(RecognitionComparisonEvaluator.FormatVersion, RecognitionComparisonEvaluator.Version,
                input.Value, EvaluationJson.Fingerprint(input.Value), result.Value, checkedTraining.Value);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new DomainError("evaluation.timeout", "Оценка превысила допустимое время; частичный отчёт не сохранён.");
        }
    }

    public static Result<RecognitionEvaluationSnapshot, DomainError> ReadSnapshot(CatalogRecognitionRuleSetReport report)
    {
        if (report.EvaluationJson is null) return new DomainError("evaluation.legacy_report", "Исторический формат: требуется новая оценка для включения.");
        try
        {
            var snapshot = JsonSerializer.Deserialize<RecognitionEvaluationSnapshot>(report.EvaluationJson, EvaluationJson.Options);
            if (snapshot is null || snapshot.FormatVersion != RecognitionComparisonEvaluator.FormatVersion ||
                !string.Equals(snapshot.EvaluatorVersion, RecognitionComparisonEvaluator.Version, StringComparison.Ordinal))
                return new DomainError("evaluation.unsupported_format", "Исполнитель или формат сравнительной оценки не поддерживается.");
            if (snapshot.Input is null || snapshot.Result is null || snapshot.Training is null || snapshot.Input.CandidateRules is null ||
                snapshot.Input.Examples is null || snapshot.Input.Definitions is null || snapshot.Input.Terms is null || snapshot.Input.Profiles is null ||
                snapshot.Input.EvidenceNames is null || snapshot.Input.Policy is null || snapshot.Result.Rows is null || snapshot.Result.Readiness is null ||
                snapshot.Input.CandidateRules.VersionId != report.RuleSetVersionId ||
                !string.Equals(snapshot.InputFingerprint, EvaluationJson.Fingerprint(snapshot.Input), StringComparison.Ordinal))
                return new DomainError("evaluation.invalid_snapshot", "Снимок оценки повреждён или относится к другой версии.");
            return snapshot;
        }
        catch (JsonException)
        {
            return new DomainError("evaluation.invalid_snapshot", "Снимок оценки не удалось прочитать.");
        }
    }

    public async Task<UnitResult<DomainError>> ValidateAsync(CatalogRecognitionRuleSetReport report, Guid userId, CancellationToken ct)
    {
        var saved = ReadSnapshot(report);
        if (saved.IsFailure) return UnitResult.Failure(saved.Error);
        var current = await capture.CaptureAsync(report.RuleSetVersionId, userId, ct).ConfigureAwait(false);
        if (current.IsFailure) return UnitResult.Failure(new DomainError("evaluation.stale", current.Error.Message));
        if (!string.Equals(saved.Value.InputFingerprint, EvaluationJson.Fingerprint(current.Value), StringComparison.Ordinal))
            return UnitResult.Failure(new DomainError("evaluation.stale", "Конфигурация, схема, контрольная выборка или политика изменились. Создайте новый сравнительный отчёт."));
        if (!string.Equals(saved.Value.Result.Readiness.State, "Ready", StringComparison.Ordinal))
            return UnitResult.Failure(new DomainError("evaluation.not_ready", string.Join(" ", saved.Value.Result.Readiness.Reasons.Select(x => x.Message))));
        return UnitResult.Success<DomainError>();
    }

    public async Task<Result<EvaluationResult, DomainError>> ReplayAsync(RecognitionEvaluationSnapshot snapshot, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try { return await evaluator.EvaluateAsync(snapshot.Input, deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return new DomainError("evaluation.timeout", "Повтор оценки превысил допустимое время."); }
    }
}
