using System.Data;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class RecognitionEvaluationReports(ElectronicDbContext db, RecognitionEvaluationAccess access,
    RecognitionEvaluationService evaluation, CatalogRecognitionRuleSetActivationValidator validator) : IRecognitionEvaluationReports
{
    public async Task<Result<EvaluationPage, DomainError>> ReadAsync(Guid reportId, int page, bool replay, CancellationToken cancellationToken)
    {
        if (page is < 1 or > 10000) return new DomainError("training.invalid_request", "Некорректная страница.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var auth = await access.AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        var report = await db.CatalogRecognitionRuleSetReports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reportId && x.CreatedByUserId == auth.Value, cancellationToken).ConfigureAwait(false);
        if (report is null) return new DomainError("training.not_found", "Отчёт не найден или недоступен.");
        var saved = RecognitionEvaluationService.ReadSnapshot(report);
        if (saved.IsFailure)
        {
            if (replay) return saved.Error;
            return new EvaluationPage(report.Id, Guid.Empty, Guid.Empty, null, report.RuleSetVersionId, 0, false, null,
                new("Stale", [new(saved.Error.Code, saved.Error.Message)]), null, null, [], 0, 0, 0, 0, null, page, 0, []);
        }
        var snapshot = saved.Value;
        var result = snapshot.Result;
        var reasons = result.Readiness.Reasons.ToList();
        var state = result.Readiness.State;
        if (!snapshot.Training.PassedTrainingChecks)
        {
            state = "Failed";
            reasons.Add(new("evaluation.training_failed", "Учебные основания версии не прошли проверку."));
        }
        if (report.ConflictRowsCount > 0 || report.ProposedRowsCount == 0)
        {
            state = "Failed";
            reasons.Add(new("evaluation.batch_failed", "В проверке пакета есть конфликты или нет предложенных значений."));
        }
        if (replay)
        {
            // The only live reads above authorize access to the stored report. Recognition uses its snapshot exclusively.
            var repeated = await evaluation.ReplayAsync(snapshot, cancellationToken).ConfigureAwait(false);
            if (repeated.IsFailure) return repeated.Error;
            result = repeated.Value;
            state = "Historical";
            reasons = [new("evaluation.historical_replay", "Воспроизведён сохранённый снимок, включая исторические источники. Это не разрешение на включение.")];
        }
        else
        {
            var valid = await validator.ValidateAsync(report.RuleSetVersionId, report.Id, cancellationToken).ConfigureAwait(false);
            if (valid.IsFailure)
            {
                if (string.Equals(valid.Error.Code, "evaluation.stale", StringComparison.Ordinal) ||
                    string.Equals(state, "Ready", StringComparison.Ordinal)) state = "Stale";
                reasons.Add(new(valid.Error.Code, valid.Error.Message));
            }
        }
        var input = snapshot.Input;
        return new EvaluationPage(report.Id, input.ManufacturerId, input.ProductTypeId, input.CurrentState.ActiveVersionId,
            input.CandidateRules!.VersionId, input.CurrentState.SequenceNumber, replay, input.Policy, new(state, reasons),
            result.Control, result.TrainingDiagnostic, result.Characteristics, result.InputExamples, result.DuplicateExamples,
            result.LabelConflicts, result.OverlapUnits, snapshot.Training.PassedTrainingChecks, page, result.Rows.Count,
            result.Rows.Skip((page - 1) * 25).Take(25).ToArray())
            { ManufacturerName = input.ManufacturerName, ProductTypeName = input.ProductTypeName };
    }
}
