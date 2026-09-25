using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class RecognitionEvaluationCapture(ElectronicDbContext db, ICatalogRecognitionRuleSetExecutionReader execution,
    ICatalogRecognitionActiveRuleSetReader active, ICatalogCharacteristicRecognitionProfileReader profiles,
    IOptions<RecognitionEvaluationOptions> options)
{
    // Caller establishes a consistent snapshot before any reads, including authorization.
    public async Task<Result<EvaluationInput, DomainError>> CaptureAsync(Guid versionId, Guid userId, CancellationToken ct)
    {
        var candidate = await execution.ReadAsync(versionId, ct).ConfigureAwait(false);
        if (candidate.IsFailure) return candidate.Error;
        return await CaptureScopeAsync(candidate.Value.ManufacturerId, candidate.Value.ProductTypeId, userId, candidate.Value, ct).ConfigureAwait(false);
    }

    public async Task<Result<EvaluationInput, DomainError>> CaptureScopeAsync(Guid manufacturerId, Guid productTypeId, Guid userId,
        CatalogRecognitionRuleSetExecutionSnapshot? candidate, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Capture requires a transaction.");
        var settings = options.Value;
        if (!settings.IsValid()) return new DomainError("evaluation.invalid_policy", "Некорректная политика оценки.");
        var scope = (ManufacturerId: manufacturerId, ProductTypeId: productTypeId);
        var state = await active.GetStateAsync(scope.ManufacturerId, scope.ProductTypeId, ct).ConfigureAwait(false);
        if (state.IsFailure) return state.Error;
        CatalogRecognitionRuleSetExecutionSnapshot? current = null;
        if (state.Value.ActiveVersionId is Guid currentId)
        {
            var loaded = await execution.ReadAsync(currentId, ct).ConfigureAwait(false);
            if (loaded.IsFailure) return loaded.Error;
            current = loaded.Value;
        }
        var manufacturer = await db.Manufacturers.AsNoTracking().Where(x => x.Id == scope.ManufacturerId).Select(x => x.Name).SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (manufacturer is null) return new DomainError("training.not_found", "Производитель не найден.");
        var productTypeName = await db.ProductTypes.AsNoTracking().Where(x => x.Id == scope.ProductTypeId).Select(x => x.Name).SingleAsync(ct).ConfigureAwait(false);
        var schema = await db.ProductTypeCharacteristics.AsNoTracking().Where(x => x.ProductTypeId == scope.ProductTypeId)
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.CharacteristicDefinitionId, x.IsRequired, x.IsFilterable, x.IsUsedForReplacement, x.ReplacementMatchMode, x.ReplacementWeight })
            .Take(1001).ToArrayAsync(ct).ConfigureAwait(false);
        if (schema.Length > 1000 || await db.CatalogCharacteristicRecognitionProfiles.AsNoTracking()
                .CountAsync(x => x.ProductTypeId == scope.ProductTypeId && x.IsActive, ct).ConfigureAwait(false) > 1000)
            return new DomainError("evaluation.selection_too_large", "Схема или число профилей превышает лимит оценки. Отчёт не создан.");
        var ids = schema.Select(x => x.CharacteristicDefinitionId).ToArray();
        var definitions = await db.CharacteristicDefinitions.AsNoTracking().Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id)
            .Select(x => new EvaluationDefinition(x.Id, x.Code, x.Name, x.DataType, x.Unit)).ToArrayAsync(ct).ConfigureAwait(false);
        var codes = definitions.Select(x => x.Code).ToArray();
        var terms = await db.CatalogDictionaryTerms.AsNoTracking().Where(x => x.Status == CatalogDictionaryTermStatus.Approved &&
            x.Kind == CatalogDictionaryTermKind.Characteristic && (x.ManufacturerId == null || x.ManufacturerId == scope.ManufacturerId) &&
            (x.ProductTypeId == null || x.ProductTypeId == scope.ProductTypeId) && codes.Contains(x.TargetCode!))
            .OrderBy(x => x.Id).Take(10001).Select(x => new CatalogDictionaryTermResult(x.Id, x.ManufacturerId, x.ProductTypeId,
                x.Phrase, x.NormalizedPhrase, x.Kind.ToString(), x.TargetCode, x.TargetValue, x.Priority, x.Status.ToString(), x.Source.ToString(),
                x.CreatedAtUtc, x.ApprovedAtUtc, null, null, null, null, null, null, null)).ToArrayAsync(ct).ConfigureAwait(false);
        var profileValues = (await profiles.GetProfilesAsync(scope.ProductTypeId, ct).ConfigureAwait(false)).Where(x => ids.Contains(x.CharacteristicDefinitionId))
            .OrderBy(x => x.Id).Select(x => x with { ConfigurationJson = EvaluationJson.Canonicalize(x.ConfigurationJson) }).ToArray();
        var examples = await db.CatalogRecognitionTrainingExamples.AsNoTracking().ConfirmedExamples(db.CatalogRecognitionFeedbackEntries)
            .Where(x => x.ConfirmedByUserId == userId && x.ManufacturerId == scope.ManufacturerId && x.ProductTypeId == scope.ProductTypeId)
            .OrderBy(x => x.Id).Take(settings.MaxExamples + 1)
            .Select(x => new EvaluationExample(x.Id, x.SourceFeedbackId, x.ProductName, x.CharacteristicDefinitionId, x.NormalizedValue, x.IsEvaluationOnly)).ToArrayAsync(ct).ConfigureAwait(false);
        var versions = new[] { candidate?.VersionId ?? Guid.Empty, state.Value.ActiveVersionId ?? Guid.Empty };
        var entries = db.CatalogRecognitionRuleSetEntries.Where(x => versions.Contains(x.VersionId));
        var evidenceExamples = db.CatalogRecognitionTrainingExamples.AsNoTracking().Where(x => x.ManufacturerId == scope.ManufacturerId && x.ProductTypeId == scope.ProductTypeId &&
            (db.CatalogRecognitionLiteralDraftEvidenceEntries.Any(e => e.TrainingExampleId == x.Id && entries.Any(v => v.LiteralDraftId == e.DraftId)) ||
             db.CatalogRecognitionIntegerDraftEvidenceEntries.Any(e => e.TrainingExampleId == x.Id && entries.Any(v => v.IntegerDraftId == e.DraftId)) ||
             db.CatalogRecognitionMultiIntegerDraftEvidenceEntries.Any(e => e.TrainingExampleId == x.Id && entries.Any(v => v.MultiIntegerDraftId == e.DraftId))));
        var evidenceNames = await evidenceExamples.Select(x => x.ProductName).Distinct().OrderBy(x => x).Take(20001).ToArrayAsync(ct).ConfigureAwait(false);
        if (examples.Length > settings.MaxExamples || terms.Length > 10000 || profileValues.Length > 1000 || definitions.Length > 1000 || evidenceNames.Length > 20000)
            return new DomainError("evaluation.selection_too_large", "Область превышает лимит оценки. Выборка не была обрезана; отчёт не создан.");
        return new EvaluationInput(scope.ManufacturerId, manufacturer, scope.ProductTypeId, state.Value, current, candidate ?? current, definitions,
            terms, profileValues, examples, evidenceNames.Select(CatalogRecognitionTextNormalizer.NormalizeText).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), settings.Policy())
            { SchemaJson = EvaluationJson.Canonicalize(EvaluationJson.Serialize(schema)), ProductTypeName = productTypeName };
    }
}
